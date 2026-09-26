namespace Struct2Zip.Models
{
    /// <summary>Một khai báo namespace/package tìm thấy trong file.</summary>
    public class NamespaceHit
    {
        public string Language { get; init; } = string.Empty;
        /// <summary>Giá trị namespace, đã normalize sang dấu chấm (kể cả PHP).</summary>
        public string Value { get; init; } = string.Empty;
        public string File  { get; init; } = string.Empty;
        public int    Line  { get; init; }
    }

    public class NamespaceScanStats
    {
        public int FilesScanned { get; init; }
        public int FilesSkipped { get; init; }
        public int TotalHits    { get; init; }
    }

    public class NamespaceScanResponse
    {
        /// <summary>
        /// Danh sách namespace duy nhất, đã normalize sang dot-notation,
        /// sort theo alphabet. Frontend hiển thị dạng chip để user click chọn.
        /// </summary>
        public List<string>       UniqueNamespaces { get; init; } = new();
        public List<NamespaceHit> Hits             { get; init; } = new();
        public NamespaceScanStats Stats            { get; init; } = new();
    }
}
