namespace Struct2Zip.Models
{
    /// <summary>
    /// Kết quả trả về khi user upload một file .zip lên POST /api/zip/import.
    /// Backend đọc cấu trúc thư mục bên trong ZIP và trả về tree text sẵn
    /// để Angular đổ vào textarea — user chỉnh rồi generate ZIP mới như bình thường.
    /// </summary>
    public class ZipImportResponse
    {
        /// <summary>Danh sách path entries đọc từ ZIP (phẳng, đã sort).</summary>
        public List<PathEntry> Entries { get; init; } = new();

        public PreviewStats Stats { get; init; } = new();

        /// <summary>
        /// Tree text đã được visualize — Angular đổ thẳng vào textarea,
        /// không cần gọi thêm /preview nữa.
        /// </summary>
        public string TreeVisualization { get; init; } = string.Empty;
    }
}
