namespace Struct2Zip.Models
{
    /// <summary>
    /// Một thao tác di chuyển file/folder trong cây — nhận từ frontend khi kéo-thả.
    /// SourcePath   : đường dẫn HIỆN TẠI của node (sau các move trước đó).
    /// TargetParentPath: thư mục đích (null = gốc ZIP).
    /// </summary>
    public class MoveRecord
    {
        public string  SourcePath        { get; set; } = string.Empty;
        public string? TargetParentPath  { get; set; }
    }
}
