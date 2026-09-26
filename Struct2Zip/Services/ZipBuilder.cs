using Microsoft.AspNetCore.Mvc;
using Struct2Zip.Models;
using System.IO.Compression;

namespace Struct2Zip.Services
{
    /// <summary>
    /// Tạo ZIP scaffold: file rỗng 0 byte, thư mục rỗng có explicit entry kết thúc '/'.
    /// Không ghi ra disk — toàn bộ trong MemoryStream.
    /// </summary>
    public class ZipBuilder : IZipBuilder
    {
        public MemoryStream Build(List<PathEntry> entries)
        {
            var ms = new MemoryStream();

            // leaveOpen: true → MemoryStream không bị đóng khi ZipArchive.Dispose() chạy,
            // để ta rewind và trả về cho caller.
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var entry in entries)
                {
                    if (entry.IsDirectory)
                    {
                        // Convention ZIP: path kết thúc '/' = directory entry.
                        // Windows Explorer, 7-Zip, unzip đều nhận diện và tạo folder rỗng.
                        zip.CreateEntry(entry.RelativePath.TrimEnd('/') + '/');
                    }
                    else
                    {
                        // CreateEntry không write data → 0-byte placeholder, đúng mục đích scaffold.
                        zip.CreateEntry(entry.RelativePath);
                    }
                }
            } // ZipArchive.Dispose() flush central directory → zip hoàn chỉnh

            ms.Position = 0; // rewind để caller đọc từ đầu
            return ms;
        }
    }
}