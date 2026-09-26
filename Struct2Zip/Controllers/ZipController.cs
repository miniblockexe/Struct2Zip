using Microsoft.AspNetCore.Mvc;
using Struct2Zip.Models;
using Struct2Zip.Services;

namespace Struct2Zip.Controllers
{
    [ApiController]
    [Route("api/zip")]
    public class ZipController : ControllerBase
    {
        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

        private readonly IZipReader               _reader;
        private readonly INamespaceScanner        _scanner;
        private readonly INamespaceRenamer        _renamer;
        private readonly IMoveAndDownloadService  _moveService;

        public ZipController(
            IZipReader              reader,
            INamespaceScanner       scanner,
            INamespaceRenamer       renamer,
            IMoveAndDownloadService moveService)
        {
            _reader      = reader;
            _scanner     = scanner;
            _renamer     = renamer;
            _moveService = moveService;
        }

        // ── POST /api/zip/import ─────────────────────────────────────────────

        [HttpPost("import")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
        public IActionResult Import(IFormFile? file)
        {
            var err = ValidateZip(file);
            if (err is not null) return err;

            try
            {
                using var stream = file!.OpenReadStream();
                var result = _reader.Read(stream);

                if (result.Entries.Count == 0)
                    return BadRequest(new { message = "File ZIP không chứa file hoặc thư mục nào hợp lệ." });

                return Ok(result);
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { message = "File không phải ZIP hợp lệ hoặc bị hỏng." });
            }
        }

        // ── POST /api/zip/scan-namespaces ────────────────────────────────────

        [HttpPost("scan-namespaces")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
        public IActionResult ScanNamespaces(IFormFile? file)
        {
            var err = ValidateZip(file);
            if (err is not null) return err;

            try
            {
                using var stream = file!.OpenReadStream();
                var result = _scanner.Scan(stream);
                return Ok(result);
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { message = "File không phải ZIP hợp lệ hoặc bị hỏng." });
            }
        }

        // ── POST /api/zip/rename-namespace ───────────────────────────────────

        [HttpPost("rename-namespace")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
        public IActionResult RenameNamespace(
            IFormFile? file,
            [FromForm] string? oldPrefix,
            [FromForm] string? newPrefix)
        {
            var err = ValidateZip(file);
            if (err is not null) return err;

            if (string.IsNullOrWhiteSpace(oldPrefix))
                return BadRequest(new { message = "Thiếu oldPrefix." });

            if (string.IsNullOrWhiteSpace(newPrefix))
                return BadRequest(new { message = "Thiếu newPrefix." });

            oldPrefix = oldPrefix.Trim();
            newPrefix = newPrefix.Trim();

            if (oldPrefix == newPrefix)
                return BadRequest(new { message = "oldPrefix và newPrefix không được giống nhau." });

            if (!System.Text.RegularExpressions.Regex.IsMatch(oldPrefix, @"^[\w.]+$") ||
                !System.Text.RegularExpressions.Regex.IsMatch(newPrefix, @"^[\w.]+$"))
                return BadRequest(new { message = "Prefix chỉ được chứa chữ cái, số, dấu chấm và gạch dưới." });

            try
            {
                using var stream = file!.OpenReadStream();
                using var result = _renamer.Rename(stream, oldPrefix, newPrefix);

                string outputName = file.FileName.Replace(".zip", $"-{newPrefix}.zip");
                return File(result.ToArray(), "application/zip", outputName);
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { message = "File không phải ZIP hợp lệ hoặc bị hỏng." });
            }
        }

        // ── POST /api/zip/move-and-download ──────────────────────────────────
        //
        // Nhận: ZIP gốc + danh sách kéo-thả (moves JSON) + cờ autoNamespace.
        // Áp tất cả moves theo thứ tự, tùy chọn sửa namespace theo đường dẫn mới,
        // trả về ZIP mới với nội dung file được giữ nguyên (hoặc đã đổi namespace).

        [HttpPost("move-and-download")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
        public IActionResult MoveAndDownload(
            IFormFile? file,
            [FromForm] string? moves,
            [FromForm] bool autoNamespace = true)
        {
            var err = ValidateZip(file);
            if (err is not null) return err;

            if (string.IsNullOrWhiteSpace(moves))
                return BadRequest(new { message = "Thiếu danh sách moves." });

            List<MoveRecord>? moveList;
            try
            {
                moveList = System.Text.Json.JsonSerializer.Deserialize<List<MoveRecord>>(
                    moves,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch
            {
                return BadRequest(new { message = "Trường moves không đúng định dạng JSON." });
            }

            if (moveList is null || moveList.Count == 0)
                return BadRequest(new { message = "Danh sách moves trống." });

            try
            {
                using var stream = file!.OpenReadStream();
                using var result = _moveService.Process(stream, moveList, autoNamespace);

                string label      = autoNamespace ? "-ns" : "-moved";
                string outputName = file.FileName.Replace(".zip", $"{label}.zip",
                                        StringComparison.OrdinalIgnoreCase);
                return File(result.ToArray(), "application/zip", outputName);
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { message = "File không phải ZIP hợp lệ hoặc bị hỏng." });
            }
        }

        // ── Helper ───────────────────────────────────────────────────────────

        private BadRequestObjectResult? ValidateZip(IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Chưa có file nào được gửi lên." });

            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Chỉ chấp nhận file .zip." });

            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new
                {
                    message = $"File vượt quá giới hạn {MaxFileSizeBytes / 1024 / 1024} MB."
                });

            return null;
        }
    }
}
