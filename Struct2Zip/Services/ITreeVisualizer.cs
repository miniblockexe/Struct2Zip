using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    public interface ITreeVisualizer
    {
        /// <summary>
        /// Chuyển List&lt;PathEntry&gt; ngược lại thành chuỗi tree text có ký tự ├── └──,
        /// dùng cho preview panel "clean output".
        /// </summary>
        string Visualize(List<PathEntry> entries);
    }
}
