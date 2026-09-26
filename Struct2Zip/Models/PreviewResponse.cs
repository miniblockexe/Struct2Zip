namespace Struct2Zip.Models
{
    public class PreviewResponse
    {
        /// <summary>Danh sách entries parse được (kể cả khi có một số dòng lỗi).</summary>
        public List<PathEntry> Entries { get; init; } = new();

        /// <summary>Các dòng lỗi — Angular dùng để highlight đỏ trong textarea.</summary>
        public List<PreviewError> Errors { get; init; } = new();

        public PreviewStats Stats { get; init; } = new();

        /// <summary>
        /// Tree text được RE-GENERATE từ entries đã parse — đây là
        /// "clean version" sẽ thực sự vào ZIP, Angular render sang panel bên phải.
        /// </summary>
        public string TreeVisualization { get; init; } = string.Empty;

        public bool HasErrors => Errors.Count > 0;
    }

    /// <param name="Line">1-based, 0 = lỗi chung.</param>
    public record PreviewError(int Line, string Message);

    public class PreviewStats
    {
        public int TotalEntries { get; init; }
        public int FileCount { get; init; }
        public int DirectoryCount { get; init; }
    }
}
