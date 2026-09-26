namespace Struct2Zip.Models
{
    /// <summary>
    /// Thrown after ALL lines have been scanned — không dừng ở lỗi đầu tiên,
    /// thu thập toàn bộ trước rồi mới throw để caller nhận đủ danh sách lỗi.
    /// </summary>
    public class TreeParseException : Exception
    {
        public IReadOnlyList<ParseError> Errors { get; }

        public TreeParseException(IEnumerable<ParseError> errors)
            : base(FormatMessage(errors))
        {
            Errors = errors.ToList().AsReadOnly();
        }

        private static string FormatMessage(IEnumerable<ParseError> errors) =>
            string.Join("; ", errors.Select(e =>
                e.LineNumber > 0 ? $"Line {e.LineNumber}: {e.Message}" : e.Message));
    }

    /// <summary>LineNumber là 1-based; 0 có nghĩa là lỗi tổng (vd: empty input).</summary>
    public record ParseError(int LineNumber, string Message);
}
