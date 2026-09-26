namespace Struct2Zip.Models
{
    public class TryParseResult
    {
        public List<PathEntry> Entries { get; init; } = new();
        public List<ParseError> Errors { get; init; } = new();
        public bool HasErrors => Errors.Count > 0;
        public bool HasEntries => Entries.Count > 0;
    }
}
