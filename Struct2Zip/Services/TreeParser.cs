using Struct2Zip.Models;
using System.Text;

namespace Struct2Zip.Services
{

    public class TreeParser : ITreeParser
    {
        // ── Constants (giữ nguyên từ version trước) ───────────────────────────
        private static readonly HashSet<char> TreeChars = new()
        { '├', '└', '─', '│', '|' };

        private static readonly HashSet<string> FileExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            ".cs", ".csproj", ".sln", ".props", ".targets",
            ".java", ".kt", ".gradle", ".xml",
            ".py", ".pyi",
            ".js", ".jsx", ".ts", ".tsx", ".mjs", ".cjs",
            ".json", ".jsonc",
            ".html", ".htm", ".css", ".scss", ".sass", ".less",
            ".md", ".mdx", ".txt", ".rst",
            ".yml", ".yaml", ".toml", ".ini", ".cfg", ".conf", ".config",
            ".sql",
            ".sh", ".bash", ".ps1", ".bat", ".cmd",
            ".rb", ".go", ".rs", ".cpp", ".c", ".h", ".hpp", ".php", ".swift",
            ".env", ".lock", ".log", ".csv", ".tsv",
            ".svg", ".png", ".jpg", ".jpeg", ".ico", ".webp",
            };

        private static readonly HashSet<string> ExtensionlessFiles =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "Dockerfile", "Makefile", "Gemfile", "Rakefile", "Procfile",
            "LICENSE", "CHANGELOG", "AUTHORS", "NOTICE", "CODEOWNERS",
            ".gitignore", ".gitattributes", ".dockerignore", ".editorconfig",
            ".nvmrc", ".npmrc", ".yarnrc", ".prettierrc", ".eslintrc", ".babelrc",
            ".env", ".htaccess",
            };

        private static readonly HashSet<char> ForbiddenChars =
            new() { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        private static readonly HashSet<string> ReservedNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "CON","PRN","AUX","NUL",
            "COM0","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT0","LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9",
            };

        // ── Public API ────────────────────────────────────────────────────────

        public List<PathEntry> Parse(string treeText)
        {
            if (string.IsNullOrWhiteSpace(treeText))
                throw new TreeParseException(new[] { new ParseError(0, "Input is empty.") });

            var (entries, errors) = ParseInternal(treeText);

            if (errors.Count > 0) throw new TreeParseException(errors);
            if (entries.Count == 0)
                throw new TreeParseException(new[]
                    { new ParseError(0, "No valid entries found in the provided tree text.") });

            return entries;
        }

        public TryParseResult TryParse(string treeText)
        {
            if (string.IsNullOrWhiteSpace(treeText))
                return new TryParseResult
                {
                    Errors = new List<ParseError> { new ParseError(0, "Input is empty.") }
                };

            var (entries, errors) = ParseInternal(treeText);
            return new TryParseResult { Entries = entries, Errors = errors };
        }

        // ── Core: chia sẻ giữa Parse() và TryParse() ─────────────────────────

        /// <summary>
        /// Hàm parse thật sự: luôn trả về cả entries và errors.
        /// Không throw — để caller quyết định xử lý thế nào.
        /// </summary>
        private static (List<PathEntry> Entries, List<ParseError> Errors) ParseInternal(string treeText)
        {
            var lines = treeText.Split('\n');
            var results = new List<PathEntry>();
            var errors = new List<ParseError>();

            var stack = new Stack<(int Col, string Path)>();
            stack.Push((-1, string.Empty)); // sentinel

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNum = i + 1;
                string raw = lines[i].TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(raw)) continue;

                var t = raw.TrimStart();
                if (t.StartsWith('#') || t.StartsWith("//")) continue;

                var (col, rawName) = MeasureAndExtract(raw);
                if (string.IsNullOrWhiteSpace(rawName)) continue;

                bool isDir = DetermineIsDirectory(rawName);
                string name = rawName.TrimEnd('/').Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var err = ValidateSegment(name, lineNum);
                if (err is not null) { errors.Add(err); continue; }

                // Stack-based depth resolution (xem comment trong version trước)
                while (stack.Count > 1 && stack.Peek().Col >= col)
                    stack.Pop();

                string parent = stack.Peek().Path;
                string fullPath = string.IsNullOrEmpty(parent) ? name : $"{parent}/{name}";

                stack.Push((col, fullPath));
                results.Add(new PathEntry { RelativePath = fullPath, IsDirectory = isDir });
            }

            return (results, errors);
        }

        // ── Helpers (giữ nguyên) ──────────────────────────────────────────────

        private static (int Col, string Name) MeasureAndExtract(string line)
        {
            var sb = new StringBuilder(line.Length);
            foreach (char c in line)
                sb.Append(TreeChars.Contains(c) || c == '\t' ? ' ' : c);

            string norm = sb.ToString();
            string trimmed = norm.TrimStart();
            return (norm.Length - trimmed.Length, trimmed.TrimEnd());
        }

        private static bool DetermineIsDirectory(string rawName)
        {
            if (rawName.EndsWith('/')) return true;
            string ext = Path.GetExtension(rawName);
            if (!string.IsNullOrEmpty(ext) && FileExtensions.Contains(ext)) return false;
            if (ExtensionlessFiles.Contains(rawName)) return false;
            return true;
        }

        private static ParseError? ValidateSegment(string segment, int lineNum)
        {
            if (segment is "." or "..")
                return new ParseError(lineNum, $"Path traversal token '{segment}' is not allowed.");

            foreach (char c in segment)
                if (ForbiddenChars.Contains(c))
                    return new ParseError(lineNum,
                        $"Character '{c}' (U+{(int)c:X4}) in '{segment}' is invalid on Windows.");

            string baseName = Path.GetFileNameWithoutExtension(segment);
            if (ReservedNames.Contains(segment) || ReservedNames.Contains(baseName))
                return new ParseError(lineNum, $"'{segment}' is a reserved Windows device name.");

            if (segment.EndsWith(' ') || segment.EndsWith('.'))
                return new ParseError(lineNum,
                    $"'{segment}' ends with a space or period — invalid on Windows.");

            return null;
        }
    }
}
