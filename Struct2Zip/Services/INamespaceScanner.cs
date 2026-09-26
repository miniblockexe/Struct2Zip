using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    public interface INamespaceScanner
    {
        /// <summary>
        /// Scan ZIP stream, tìm tất cả khai báo namespace/package trong các file
        /// C# / Java / Kotlin / PHP. Stream không bị đóng — caller chịu trách nhiệm dispose.
        /// </summary>
        NamespaceScanResponse Scan(Stream zipStream);
    }
}
