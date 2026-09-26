using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    public interface IZipBuilder
    {
        /// <summary>
        /// Tạo ZIP trong memory từ danh sách entries.
        /// Caller chịu trách nhiệm dispose stream trả về.
        /// </summary>
        MemoryStream Build(List<PathEntry> entries);
    }
}
