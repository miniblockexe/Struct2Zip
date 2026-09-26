using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    public interface IZipReader
    {
        /// <summary>
        /// Đọc một ZIP stream, trích xuất cấu trúc thư mục và trả về
        /// ZipImportResponse (entries + tree visualization).
        /// Stream không bị đóng — caller chịu trách nhiệm dispose.
        /// </summary>
        ZipImportResponse Read(Stream zipStream);
    }
}
