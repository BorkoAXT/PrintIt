using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintIt.Data;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.ViewModels;
using System.Security.Claims;

namespace PrintIt.Controllers
{
    public class StoreController : BaseController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StoreController(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context) : base(context)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: store
        [HttpGet("Store")]
        public async Task<IActionResult> Articles(StoreSearchViewModel filter)
        {
            // Check if there are any query parameters (excluding the handler parameter used by Razor Pages)
            // Use this to determine if we should apply filters or show the default view with sections
            bool hasActiveFilters = Request.Query.Keys.Any(k => k != "handler");

            var query = _context.Prints.AsQueryable();

            if (hasActiveFilters)
            {
                // Apply filters based on the provided search criteria
                if (!string.IsNullOrEmpty(filter.SearchString))
                {
                    query = query.Where(p => p.Name.Contains(filter.SearchString) || p.Description.Contains(filter.SearchString));
                }

                if (filter.SelectedType.HasValue)
                {
                    query = query.Where(p => p.PrintType == filter.SelectedType.Value);
                }

                if (filter.SelectedMaterial.HasValue)
                {
                    query = query.Where(p => p.MaterialType == filter.SelectedMaterial.Value);
                }

                if (filter.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= filter.MinPrice.Value);
                }

                if (filter.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= filter.MaxPrice.Value);
                }

                filter.Results = await query.OrderByDescending(p => p.AddedOn).ToListAsync();
                ViewData["IsFiltering"] = true;
            }
            else
            {
                // Default view with recent items in each category
                var allItems = await _context.Prints.OrderByDescending(p => p.AddedOn).ToListAsync();

                filter.RecentFigures = allItems.Where(i => i.PrintType == PrintType.Figure).Take(4).ToList();
                filter.RecentFidgetToys = allItems.Where(i => i.PrintType == PrintType.FidgetToy).Take(4).ToList();
                filter.RecentAccessories = allItems.Where(i => i.PrintType == PrintType.Accessory).Take(4).ToList();

                ViewData["IsFiltering"] = false;
            }

            return View(filter);
        }

        // GET: store/category/{type}
        [HttpGet("Store/Category/{type}")]
        public async Task<IActionResult> Category(string type)
        {
            // TryParse the string into PrintType enum, ignore case
            if (!Enum.TryParse(type, true, out PrintType categoryEnum))
            {
                return RedirectToAction("Articles"); // If parsing fails, redirect to the main articles page
            }

            var items = await _context.Prints
                .Where(p => p.PrintType == categoryEnum)
                .OrderByDescending(p => p.AddedOn)
                .ToListAsync();

            ViewData["CategoryName"] = categoryEnum switch
            {
                PrintType.Figure => "Фигурки",
                PrintType.FidgetToy => "Играчки",
                PrintType.Accessory => "Аксесоари",
                _ => "Артикули"
            };

            return View(items);
        }

        // GET: store/details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var print = await _context.Prints.FindAsync(id);
            if (print == null) return NotFound();

            bool isFavorite = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    // Check if the current print is in the user's wishlist
                    isFavorite = await _context.Set<UserWishlistItem>()
                        .AnyAsync(w => w.PrintId == id && w.UserId == userId);
                }
            }

            // Use ViewBag to pass the isFavorite value to the view
            ViewBag.IsFavorite = isFavorite;

            return View(print);
        }

        // GET: store/add
        [HttpGet("Store/Add")]
        [Authorize(Roles = "Admin")]
        public IActionResult Add3DPrint()
        {
            return View("~/Views/Store/Add3DPrint.cshtml", new PrintsViewModel());
        }

        // POST: store/add
        [HttpPost("Store/Add")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add3DPrint(PrintsViewModel model)
        {
            if (ModelState.IsValid)
            {
                string filePath = "/images/placeholder-figure.jpg"; // Default placeholder image

                if (model.File != null && model.File.Length > 0)
                {
                    filePath = await UploadImage(model.File, model.PrintType);
                }

                var newPrint = new Print(model.Name, model.Description, model.MaterialType,
                    model.Weight, model.Price, filePath,
                    model.PrintType, model.PrintColors);

                _context.Prints.Add(newPrint);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Articles));
            }
            return View("~/Views/Store/Add3DPrint.cshtml", model);
        }

        // GET: store/edit/{id}
        [HttpGet("Store/Edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var print = await _context.Prints.FindAsync(id);
            if (print == null) return NotFound();

            var viewModel = new PrintsViewModel
            {
                Id = print.Id,
                Name = print.Name,
                Description = print.Description,
                Price = print.Price,
                Weight = print.Weight,
                MaterialType = print.MaterialType,
                PrintType = print.PrintType,
                PrintColors = print.PrintColors ?? new List<Enums.PrintColor>(),
                FilePath = print.FilePath
            };

            return View("~/Views/Store/Edit.cshtml", viewModel);
        }

        // POST: store/edit/{id}
        [HttpPost("Store/Edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid id, PrintsViewModel model)
        {
            if (id != model.Id) return BadRequest();

            ModelState.Remove("File");

            if (ModelState.IsValid)
            {
                var existingPrint = await _context.Prints.FindAsync(id);
                if (existingPrint == null) return NotFound();

                existingPrint.Name = model.Name;
                existingPrint.Description = model.Description;
                existingPrint.Price = model.Price;
                existingPrint.Weight = model.Weight;
                existingPrint.MaterialType = model.MaterialType;
                existingPrint.PrintType = model.PrintType;
                existingPrint.PrintColors = model.PrintColors;

                if (model.File != null && model.File.Length > 0)
                {
                    DeleteOldImage(existingPrint.FilePath);
                    existingPrint.FilePath = await UploadImage(model.File, model.PrintType);
                }

                _context.Update(existingPrint);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Articles));
            }
            return View(model);
        }

        // POST: store/delete/{id}
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var print = await _context.Prints.FindAsync(id);
            if (print != null)
            {
                DeleteOldImage(print.FilePath);
                _context.Prints.Remove(print);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Articles));
        }

        // Helper методи за снимките
        private async Task<string> UploadImage(IFormFile file, PrintType printType)
        {
            string categoryFolder = string.Empty;
            switch (printType)
            {
                case PrintType.Accessory: categoryFolder = "Accessories"; break;
                case PrintType.FidgetToy: categoryFolder = "FidgetToys"; break;
                case PrintType.Figure: categoryFolder = "Figures"; break;
            };

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", categoryFolder);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return $"/images/{categoryFolder}/{uniqueFileName}";
        }

        private void DeleteOldImage(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || filePath.Contains("placeholder")) return;

            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWishlist(Guid id)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out Guid userId)) return Json(new { success = false });

            var wishlistItem = await _context.Set<UserWishlistItem>()
                .FirstOrDefaultAsync(w => w.PrintId == id && w.UserId == userId);

            if (wishlistItem != null)
            {
                _context.Set<UserWishlistItem>().Remove(wishlistItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false });
            }
            else
            {
                _context.Set<UserWishlistItem>().Add(new UserWishlistItem (userId, id));
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true });
            }
        }
    }
}