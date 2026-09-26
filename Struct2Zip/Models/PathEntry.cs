namespace Struct2Zip.Models;

public class PathEntry
{
    /// <summary>
    /// Full relative path from root using '/' separator, no trailing slash.
    /// Example: "HealthPlus.API/Controllers/AuthController.cs"
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>True = directory, False = file.</summary>
    public bool IsDirectory { get; set; }
}