using System.ComponentModel.DataAnnotations;

namespace Struct2Zip.Models
{
    public class GenerateRequest
    {
        [Required]
        public string TreeText { get; set; } = string.Empty;
    }
}
