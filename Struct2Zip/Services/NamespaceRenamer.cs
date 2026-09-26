using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Struct2Zip.Services
{
    public class NamespaceRenamer : INamespaceRenamer
    {
        // Extension cần xử lý nội dung (text), còn lại copy nguyên
        private static readonly HashSet<string> CodeExts =
            new(StringComparer.OrdinalIgnoreCase)
            { ".cs", ".java", ".kt", ".kts", ".php" };

        private static readonly HashSet<string> PhpExts =
            new(StringComparer.OrdinalIgnoreCase) { ".php" };

        public MemoryStream Rename(Stream zipStream, string oldPrefix, string newPrefix)
        {
            // ── Build patterns ────────────────────────────────────────────────
            //
            // Dot pattern  (C# / Java / Kotlin):
            //   \bOldPrefix(?=[.;\s{<(\[]|$)
            //   Lookahead: dấu chấm tiếp theo, dấu ; cuối câu, khoảng trắng,
            //   dấu mở block/generic/paren/array, hoặc cuối chuỗi.
            //
            // PHP pattern — user luôn nhập dạng dot, ta convert sang backslash:
            //   \bFoo\Bar(?=[\\;\s{(]|$)

            string escapedDot = Regex.Escape(oldPrefix);
            var dotPattern = new Regex(
                $@"\b{escapedDot}(?=[.;\s{{<(\[]|$)",
                RegexOptions.Compiled | RegexOptions.Multiline);

            string phpOld     = oldPrefix.Replace('.', '\\');
            string phpNew     = newPrefix.Replace('.', '\\');
            string escapedPhp = Regex.Escape(phpOld);
            var phpPattern = new Regex(
                $@"\b{escapedPhp}(?=[\\;\s{{(]|$)",
                RegexOptions.Compiled | RegexOptions.Multiline);

            // ── Ghi ZIP mới vào MemoryStream ─────────────────────────────────
            var output = new MemoryStream();

            using (var outZip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            using (var inZip  = new ZipArchive(zipStream, ZipArchiveMode.Read,  leaveOpen: true))
            {
                foreach (var entry in inZip.Entries)
                {
                    // Directory entry — tạo lại nguyên
                    if (entry.FullName.EndsWith('/'))
                    {
                        outZip.CreateEntry(entry.FullName);
                        continue;
                    }

                    var outEntry = outZip.CreateEntry(entry.FullName,
                                                      CompressionLevel.Fastest);

                    using var inStream  = entry.Open();
                    using var outStream = outEntry.Open();

                    string ext = Path.GetExtension(entry.Name).ToLowerInvariant();
                    if (!CodeExts.Contains(ext))
                    {
                        // Binary / file không phải code → copy nguyên
                        inStream.CopyTo(outStream);
                        continue;
                    }

                    // ── Đọc nội dung text ──────────────────────────────────
                    using var reader  = new StreamReader(
                        inStream,
                        detectEncodingFromByteOrderMarks: true,
                        leaveOpen: false);
                    string content  = reader.ReadToEnd();
                    var    encoding = reader.CurrentEncoding;

                    // ── Replace ────────────────────────────────────────────
                    string modified = PhpExts.Contains(ext)
                        ? phpPattern.Replace(content, _ => phpNew)
                        : dotPattern.Replace(content, _ => newPrefix);

                    // ── Ghi lại (giữ encoding gốc) ─────────────────────────
                    byte[] bytes = encoding.GetBytes(modified);
                    outStream.Write(bytes, 0, bytes.Length);
                }
            }

            output.Position = 0;
            return output;
        }
    }
}
