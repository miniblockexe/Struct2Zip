using System.IO.Compression;
using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    /// <summary>
    /// Đọc cấu trúc thư mục bên trong một file ZIP đã upload.
    /// Không đọc nội dung file — chỉ quan tâm tới tên đường dẫn.
    /// </summary>
    public class ZipReader : IZipReader
    {
        private readonly ITreeVisualizer _visualizer;

        public ZipReader(ITreeVisualizer visualizer)
        {
            _visualizer = visualizer;
        }

        public ZipImportResponse Read(Stream zipStream)
        {
            var entries = new List<PathEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

            foreach (var zipEntry in zip.Entries)
            {
                // Normalize: một số tool tạo ZIP dùng backslash (Windows)
                string fullName = zipEntry.FullName.Replace('\\', '/');

                // Lọc artifact của macOS
                if (fullName.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (fullName.Contains("/__MACOSX/", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Lọc file hệ thống ẩn
                string fileName = Path.GetFileName(fullName.TrimEnd('/'));
                if (fileName.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (fileName.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isDir = fullName.EndsWith('/');
                string normalizedPath = fullName.TrimEnd('/');

                if (string.IsNullOrWhiteSpace(normalizedPath)) continue;

                // Đảm bảo tất cả thư mục cha đều có mặt trong danh sách
                // (một số ZIP chỉ chứa file, không có explicit directory entry)
                EnsureParents(normalizedPath, seen, entries);

                if (seen.Add(normalizedPath))
                {
                    entries.Add(new PathEntry
                    {
                        RelativePath = normalizedPath,
                        IsDirectory = isDir,
                    });
                }
            }

            // Sort: thư mục cha trước con, alphabetical trong cùng cấp
            entries = entries
                .OrderBy(e => e.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            string treeVisualization = entries.Count > 0
                ? _visualizer.Visualize(entries)
                : string.Empty;

            return new ZipImportResponse
            {
                Entries = entries,
                Stats = new PreviewStats
                {
                    TotalEntries = entries.Count,
                    FileCount    = entries.Count(e => !e.IsDirectory),
                    DirectoryCount = entries.Count(e => e.IsDirectory),
                },
                TreeVisualization = treeVisualization,
            };
        }

        /// <summary>
        /// Với đường dẫn "a/b/c/file.txt", thêm "a", "a/b", "a/b/c" vào danh sách
        /// nếu chúng chưa có — để TreeVisualizer render đúng cây phân cấp.
        /// </summary>
        private static void EnsureParents(
            string path,
            HashSet<string> seen,
            List<PathEntry> entries)
        {
            var parts = path.Split('/');
            // Chỉ lặp đến parts.Length - 1 (bỏ tên file/thư mục chính)
            for (int i = 1; i < parts.Length; i++)
            {
                string parentPath = string.Join('/', parts.Take(i));
                if (seen.Add(parentPath))
                {
                    entries.Add(new PathEntry
                    {
                        RelativePath = parentPath,
                        IsDirectory  = true,
                    });
                }
            }
        }
    }
}
