using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    public class MoveAndDownloadService : IMoveAndDownloadService
    {
        private static readonly HashSet<string> CodeExts =
            new(StringComparer.OrdinalIgnoreCase)
            { ".cs", ".java", ".kt", ".kts", ".php" };

        private static readonly HashSet<string> PhpExts =
            new(StringComparer.OrdinalIgnoreCase) { ".php" };

        // ─────────────────────────────────────────────────────────────────────
        public MemoryStream Process(Stream zipStream, List<MoveRecord> moves, bool autoNamespace)
        {
            // ── Bước 1: Load ZIP vào dictionary path → bytes ─────────────────
            var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);

            using (var inZip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in inZip.Entries)
                {
                    if (entry.FullName.EndsWith('/')) continue; // bỏ directory entry
                    using var ms = new MemoryStream((int)entry.Length);
                    using var s  = entry.Open();
                    s.CopyTo(ms);
                    entries[entry.FullName] = ms.ToArray();
                }
            }

            // ── Bước 2: Áp từng move — cập nhật dict + thu thập ns renames ──
            //
            // Mỗi move được xử lý theo thứ tự, vì sourcePath ở move N
            // là path HIỆN TẠI sau khi move 0..N-1 đã áp.
            // Điều này cho phép chain: A→B rồi B→C hoạt động đúng.

            var nsRenames = new List<(string OldNs, string NewNs)>();

            foreach (var move in moves)
            {
                string src         = Normalize(move.SourcePath);
                string dstParent   = move.TargetParentPath != null
                                         ? Normalize(move.TargetParentPath)
                                         : "";
                string baseName    = GetBaseName(src);
                string newBase     = dstParent.Length > 0
                                         ? $"{dstParent}/{baseName}"
                                         : baseName;

                if (src == newBase) continue; // thả vào đúng vị trí cũ → no-op

                // ── Tìm tất cả entry cần di chuyển (file hoặc cả cây folder) ─
                var toMove = entries.Keys
                    .Where(k => k == src || k.StartsWith(src + "/", StringComparison.Ordinal))
                    .ToList();

                if (toMove.Count == 0) continue; // không có gì để di chuyển

                // Entry tồn tại CHÍNH XÁC với đường dẫn src → là file
                bool isFile = entries.ContainsKey(src);

                foreach (var oldKey in toMove)
                {
                    string relTail = oldKey.Length > src.Length
                                         ? oldKey[src.Length..] // "/sub/path" hoặc ""
                                         : "";
                    string newKey  = newBase + relTail;

                    var bytes = entries[oldKey];
                    entries.Remove(oldKey);
                    entries[newKey] = bytes;
                }

                // ── Suy ra namespace cũ/mới từ đường dẫn folder ─────────────
                if (autoNamespace)
                {
                    // File: namespace theo folder chứa nó
                    // Folder: namespace theo chính đường dẫn folder
                    string oldNsFolder = isFile ? GetParent(src)     : src;
                    string newNsFolder = isFile ? dstParent            : newBase;

                    if (!string.IsNullOrEmpty(oldNsFolder))
                    {
                        string oldNs = PathToNs(oldNsFolder);
                        string newNs = PathToNs(newNsFolder);
                        if (oldNs != newNs && !nsRenames.Any(r => r.OldNs == oldNs))
                            nsRenames.Add((oldNs, newNs));
                    }
                }
            }

            // ── Bước 3: Áp namespace renames vào TẤT CẢ file code ───────────
            if (autoNamespace && nsRenames.Count > 0)
            {
                // Compile patterns (dot-notation cho C#/Java/Kotlin,
                //                   backslash-notation cho PHP)
                var patterns = nsRenames.Select(r =>
                {
                    string phpOld = r.OldNs.Replace('.', '\\');
                    string phpNew = r.NewNs.Replace('.', '\\');
                    return (
                        r.NewNs,
                        phpNew,
                        DotPat: new Regex(
                            $@"\b{Regex.Escape(r.OldNs)}(?=[.;\s{{<(\[]|$)",
                            RegexOptions.Compiled | RegexOptions.Multiline),
                        PhpPat: new Regex(
                            $@"\b{Regex.Escape(phpOld)}(?=[\\;\s{{(]|$)",
                            RegexOptions.Compiled | RegexOptions.Multiline)
                    );
                }).ToList();

                foreach (var key in entries.Keys.ToList())
                {
                    string ext = Path.GetExtension(key).ToLowerInvariant();
                    if (!CodeExts.Contains(ext)) continue;

                    bool   isPhp = PhpExts.Contains(ext);
                    string text  = Encoding.UTF8.GetString(entries[key]);
                    string result = text;

                    foreach (var (newNs, phpNew, dotPat, phpPat) in patterns)
                        result = isPhp
                            ? phpPat.Replace(result, _ => phpNew)
                            : dotPat.Replace(result, _ => newNs);

                    if (!ReferenceEquals(result, text))
                        entries[key] = Encoding.UTF8.GetBytes(result);
                }
            }

            // ── Bước 4: Đóng gói ZIP output ─────────────────────────────────
            var output = new MemoryStream();
            using (var outZip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (path, data) in entries.OrderBy(kv => kv.Key))
                {
                    var outEntry = outZip.CreateEntry(path, CompressionLevel.Fastest);
                    using var s  = outEntry.Open();
                    s.Write(data, 0, data.Length);
                }
            }

            output.Position = 0;
            return output;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string Normalize(string path) =>
            path.Replace('\\', '/').Trim('/');

        private static string GetBaseName(string path)
        {
            int i = path.LastIndexOf('/');
            return i < 0 ? path : path[(i + 1)..];
        }

        private static string GetParent(string path)
        {
            int i = path.LastIndexOf('/');
            return i < 0 ? "" : path[..i];
        }

        /// <summary>
        /// Chuyển đường dẫn folder sang dot-notation namespace.
        /// "Src/Dto/User" → "Src.Dto.User"
        /// </summary>
        private static string PathToNs(string path) =>
            path.Replace('/', '.').Replace('-', '_');
    }
}
