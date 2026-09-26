namespace Struct2Zip.Services
{
    public interface INamespaceRenamer
    {
        /// <summary>
        /// Đọc ZIP đầu vào, thay thế namespace prefix trong tất cả file code
        /// (C# / Java / Kotlin / PHP), trả về MemoryStream chứa ZIP mới.
        /// oldPrefix và newPrefix luôn dùng dấu chấm — backend tự convert
        /// sang backslash khi xử lý file PHP.
        /// </summary>
        MemoryStream Rename(Stream zipStream, string oldPrefix, string newPrefix);
    }
}
