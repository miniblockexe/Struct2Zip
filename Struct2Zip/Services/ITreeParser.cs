using Struct2Zip.Models;

namespace Struct2Zip.Services
{
    public interface ITreeParser
    {
        /// <summary>Strict mode: throw TreeParseException nếu có BẤT KỲ lỗi nào.</summary>
        /// <exception cref="TreeParseException"/>
        List<PathEntry> Parse(string treeText);

        /// <summary>
        /// Lenient mode: luôn trả về kết quả, kể cả khi có lỗi.
        /// Dùng cho preview — không throw.
        /// </summary>
        TryParseResult TryParse(string treeText);
    }
}
