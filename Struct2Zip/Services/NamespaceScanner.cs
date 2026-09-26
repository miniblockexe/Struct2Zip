using System.IO.Compression;
using System.Text.RegularExpressions;
using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    public class NamespaceScanner : INamespaceScanner
    {
        // (language, extensions[], regex bắt khai báo namespace/package)
        // Group 1 = giá trị namespace thô
        private static readonly (string Lang, string[] Exts, Regex Decl)[] Rules =
        [
            (
                "C#",
                [".cs"],
                new Regex(@"^\s*namespace\s+([\w.]+)", RegexOptions.Compiled)
            ),
            (
                "Java",
                [".java"],
                new Regex(@"^\s*package\s+([\w.]+)\s*;", RegexOptions.Compiled)
            ),
            (
                "Kotlin",
                [".kt", ".kts"],
                new Regex(@"^\s*package\s+([\w.]+)", RegexOptions.Compiled)
            ),
            (
                "PHP",
                [".php"],
                // PHP dùng \ làm separator: namespace App\Controllers;
                new Regex(@"^\s*namespace\s+([\w\\]+)\s*;?", RegexOptions.Compiled)
            ),
        ];

        // Thư mục không cần scan — build output, vendor, git...
        private static readonly HashSet<string> SkippedDirs =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "node_modules", "bin", "obj", "vendor", ".git",
                "dist", "build", "out", "__pycache__", ".idea", ".vs",
            };

        public NamespaceScanResponse Scan(Stream zipStream)
        {
            var hits    = new List<NamespaceHit>();
            int scanned = 0;
            int skipped = 0;

            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

            foreach (var entry in zip.Entries)
            {
                string fullName = entry.FullName.Replace('\\', '/');

                if (fullName.EndsWith('/'))               { skipped++; continue; }
                if (fullName.StartsWith("__MACOSX/"))     { skipped++; continue; }
                if (IsInSkippedDir(fullName))             { skipped++; continue; }

                var rule = FindRule(fullName);
                if (rule is null)                         { skipped++; continue; }

                try
                {
                    using var reader = new StreamReader(entry.Open());
                    int lineNum = 0;
                    string? line;

                    while ((line = reader.ReadLine()) is not null)
                    {
                        lineNum++;
                        var trimmed = line.TrimStart();

                        // Bỏ qua dòng comment
                        if (trimmed.StartsWith("//") ||
                            trimmed.StartsWith("*")  ||
                            trimmed.StartsWith("#")  ||
                            trimmed.StartsWith("<!--"))
                            continue;

                        var match = rule.Value.Decl.Match(line);
                        if (!match.Success) continue;

                        string raw        = match.Groups[1].Value;
                        // Normalize PHP backslash → dot để hiển thị nhất quán
                        string normalized = rule.Value.Lang == "PHP"
                            ? raw.Replace('\\', '.')
                            : raw;

                        hits.Add(new NamespaceHit
                        {
                            Language = rule.Value.Lang,
                            Value    = normalized,
                            File     = fullName,
                            Line     = lineNum,
                        });
                    }
                    scanned++;
                }
                catch { skipped++; }
            }

            var unique = hits
                .Select(h => h.Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(v => v)
                .ToList();

            return new NamespaceScanResponse
            {
                UniqueNamespaces = unique,
                Hits             = hits,
                Stats = new NamespaceScanStats
                {
                    FilesScanned = scanned,
                    FilesSkipped = skipped,
                    TotalHits    = hits.Count,
                },
            };
        }

        private static bool IsInSkippedDir(string path)
            => path.Split('/').Any(p => SkippedDirs.Contains(p));

        private static (string Lang, string[] Exts, Regex Decl)? FindRule(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            foreach (var rule in Rules)
                if (rule.Exts.Contains(ext)) return rule;
            return null;
        }
    }
}
