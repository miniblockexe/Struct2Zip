using Microsoft.AspNetCore.Mvc;
using Struct2Zip.Models;
using Struct2Zip.Services;

namespace Struct2Zip.Controllers
{
    [ApiController]
    [Route("api/generate")]
    public class TreeController : ControllerBase
    {
        private readonly ITreeParser _parser;
        private readonly IZipBuilder _builder;
        private readonly ITreeVisualizer _visualizer;

        public TreeController(ITreeParser parser, IZipBuilder builder, ITreeVisualizer visualizer)
        {
            _parser = parser;
            _builder = builder;
            _visualizer = visualizer;
        }

        // ── POST /api/generate ── (giữ nguyên) ───────────────────────────────
        [HttpPost]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Generate([FromBody] GenerateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.TreeText))
                return BadRequest(new { message = "treeText is required and must not be empty." });

            try
            {
                var entries = _parser.Parse(request.TreeText);
                var zipStream = _builder.Build(entries);
                return File(zipStream, "application/zip", "structure.zip");
            }
            catch (TreeParseException ex)
            {
                return BadRequest(new
                {
                    message = "Tree structure could not be parsed.",
                    errors = ex.Errors.Select(e => new { line = e.LineNumber, detail = e.Message }),
                });
            }
        }

        // ── POST /api/generate/preview ── MỚI ────────────────────────────────
        /// <summary>
        /// Parse tree text và trả về:
        /// - entries: danh sách file/folder sẽ vào ZIP
        /// - errors:  các dòng lỗi (Angular dùng để highlight)
        /// - stats:   thống kê nhanh
        /// - treeVisualization: clean tree text re-generated (panel preview bên phải)
        ///
        /// KHÔNG tạo ZIP — chỉ để xem và sửa.
        /// </summary>
        [HttpPost("preview")]
        [ProducesResponseType(typeof(PreviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Preview([FromBody] GenerateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.TreeText))
                return BadRequest(new { message = "treeText is required." });

            // TryParse: không throw, trả về kết quả partial + errors
            var result = _parser.TryParse(request.TreeText);

            // Nếu hoàn toàn không parse được gì cả (input rác)
            if (!result.HasEntries && !result.HasErrors)
                return BadRequest(new { message = "No valid content found." });

            // Re-generate tree visualization từ entries đã parse thành công
            string visualization = result.HasEntries
                ? _visualizer.Visualize(result.Entries)
                : string.Empty;

            return Ok(new PreviewResponse
            {
                Entries = result.Entries,
                Errors = result.Errors
                               .Select(e => new PreviewError(e.LineNumber, e.Message))
                               .ToList(),
                Stats = new PreviewStats
                {
                    TotalEntries = result.Entries.Count,
                    FileCount = result.Entries.Count(e => !e.IsDirectory),
                    DirectoryCount = result.Entries.Count(e => e.IsDirectory),
                },
                TreeVisualization = visualization,
            });
        }

        // ── GET /api/generate/sample ── (giữ nguyên) ─────────────────────────
        [HttpGet("sample")]
        public IActionResult GetSample()
        {
            const string sample =
                "HealthPlus.API/\n" +
                "├── Controllers/\n" +
                "│   ├── AuthController.cs\n" +
                "│   └── UsersController.cs\n" +
                "├── Services/\n" +
                "│   ├── IAuthService.cs\n" +
                "│   └── AuthService.cs\n" +
                "├── Models/\n" +
                "│   ├── User.cs\n" +
                "│   └── LoginRequest.cs\n" +
                "├── Data/\n" +
                "│   └── AppDbContext.cs\n" +
                "├── Migrations/\n" +
                "├── Properties/\n" +
                "│   └── launchSettings.json\n" +
                "├── appsettings.json\n" +
                "├── appsettings.Development.json\n" +
                "└── Program.cs";

            return Ok(new { sample });
        }
    }
}
