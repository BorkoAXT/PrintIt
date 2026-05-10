using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers
{
    [Route("media")]
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MediaController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("{id:guid}/{fileName?}")]
        public async Task<IActionResult> Get(Guid id, string? fileName = null)
        {
            var media = await _db.PrintMedia
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.Id == id);

            if (media == null)
            {
                return NotFound();
            }

            return File(media.Data, media.ContentType, enableRangeProcessing: true);
        }
    }
}