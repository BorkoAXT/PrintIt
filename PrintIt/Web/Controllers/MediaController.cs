using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Web.Controllers
{
    [Route("media")]
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MediaController> _logger;

        public MediaController(ApplicationDbContext db, ILogger<MediaController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("{id:guid}/{fileName?}")]
        public async Task<IActionResult> Get(Guid id, string? fileName = null)
        {
            try
            {
                var media = await _db.PrintMedia
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pm => pm.Id == id);

                if (media == null)
                {
                    _logger.LogWarning("Media file not found: {MediaId}", id);
                    return NotFound();
                }

                if (media.Data == null || media.Data.Length == 0)
                {
                    _logger.LogWarning("Media file has no data: {MediaId}", id);
                    return NotFound();
                }

                // Stream the file instead of loading entirely into memory
                var stream = new MemoryStream(media.Data, writable: false);
                
                // Use Content-Disposition to set filename for download
                var displayName = string.IsNullOrWhiteSpace(fileName) 
                    ? $"file{media.Extension}"
                    : Uri.UnescapeDataString(fileName);

                return File(stream, media.ContentType, displayName, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving media file: {MediaId}", id);
                return StatusCode(500, "Error retrieving file");
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var media = await _db.PrintMedia
                    .FirstOrDefaultAsync(pm => pm.Id == id);

                if (media == null)
                {
                    _logger.LogWarning("Media file not found for deletion: {MediaId}", id);
                    return NotFound(new { message = "File not found" });
                }

                _db.PrintMedia.Remove(media);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Deleted media file: {MediaId}, Size: {Size} bytes", id, media.Data?.Length ?? 0);
                return Ok(new { message = "File deleted successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error deleting media file: {MediaId}", id);
                return StatusCode(500, new { message = "Database error while deleting file", error = dbEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting media file: {MediaId}", id);
                return StatusCode(500, new { message = "Error deleting file", error = ex.Message });
            }
        }
    }
}