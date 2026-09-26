using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    public interface IMoveAndDownloadService
    {
        /// <summary>
        /// Xử lý ZIP gốc:
        ///   1. Áp danh sách moves (kéo-thả) theo thứ tự — đổi đường dẫn entry trong ZIP.
        ///   2. Nếu autoNamespace = true: suy ra namespace cũ/mới từ đường dẫn folder,
        ///      rồi replace toàn bộ file code (.cs / .java / .kt / .php) trong ZIP.
        /// Trả về ZIP mới dưới dạng MemoryStream (Position = 0).
        /// </summary>
        MemoryStream Process(Stream zipStream, List<MoveRecord> moves, bool autoNamespace);
    }
}
