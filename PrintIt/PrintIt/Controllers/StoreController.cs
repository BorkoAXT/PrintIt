using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.Services;
using PrintIt.ViewModels;
using System.Security.Claims;

public class StoreController : Controller
{
    private readonly ILogger<StoreController> _logger;
    private readonly IPrintService _printService;
    private readonly IFileService _fileService;
    private readonly IWishlistService _wishlistService;
    private readonly ICartService _cartService;

    public StoreController(
        ILogger<StoreController> logger,
        IPrintService printService,
        IFileService fileService,
        IWishlistService wishlistService,
        ICartService cartService)
    {
        _logger = logger;
        _printService = printService;
        _fileService = fileService;
        _wishlistService = wishlistService;
        _cartService = cartService;
    }

    [HttpGet("Store")]
    public async Task<IActionResult> Articles(StoreSearchViewModel filter)
    {
        _logger.LogDebug("Accessing Store Articles. Filter applied: {IsFiltered}", Request.Query.Keys.Any());

        bool hasFilters = Request.Query.Keys.Any(k => k != "handler");

        if (hasFilters)
        {
            filter.Results = await _printService.SearchAsync(filter);
            ViewData["IsFiltering"] = true;
        }
        else
        {
            var allItems = await _printService.GetAllAsync();
            filter.RecentFigures = allItems.Where(p => p.PrintType == PrintType.Figure).Take(4).ToList();
            filter.RecentFidgetToys = allItems.Where(p => p.PrintType == PrintType.FidgetToy).Take(4).ToList();
            filter.RecentAccessories = allItems.Where(p => p.PrintType == PrintType.Accessory).Take(4).ToList();
            ViewData["IsFiltering"] = false;
        }

        return View(filter);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var print = await _printService.GetByIdAsync(id);
        if (print == null)
        {
            _logger.LogWarning("Product Details requested for non-existent ID: {PrintId}", id);
            return NotFound();
        }

        bool isFavorite = false;
        if (TryGetUserId(out Guid userId))
            isFavorite = await _wishlistService.IsFavoriteAsync(userId, id);

        ViewBag.IsFavorite = isFavorite;
        return View(print);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWishlist(Guid id)
    {
        if (!TryGetUserId(out Guid userId)) return Json(new { success = false, message = "Unauthorized" });

        bool isFavorite = await _wishlistService.ToggleAsync(userId, id);
        _logger.LogInformation("User {UserId} toggled wishlist for Print {PrintId}. New State: {IsFavorite}", userId, id, isFavorite);

        return Json(new { success = true, isFavorite });
    }

    [HttpGet("Store/Add")]
    [Authorize(Roles = "Admin")]
    public IActionResult Add3DPrint()
    {
        return View("~/Views/Store/Add3DPrint.cshtml", new PrintsViewModel());
    }

    // --- ADMIN OPERATIONS ---

    [HttpPost("Store/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add3DPrint(PrintsViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            string filePath = "/images/placeholder-figure.jpg";
            if (model.File != null)
            {
                filePath = await _fileService.UploadImageAsync(model.File, model.PrintType);
                _logger.LogInformation("Admin uploaded new image: {FilePath}", filePath);
            }

            var print = new Print(
                model.Name, model.Description, model.MaterialType, model.Weight,
                model.Price, filePath, model.PrintType, model.PrintColors
            );

            await _printService.AddAsync(print);
            _logger.LogInformation("Admin created new product: {PrintName} ({PrintId})", print.Name, print.Id);

            return RedirectToAction(nameof(Articles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new 3D print: {PrintName}", model.Name);
            return View(model);
        }
    }

    [HttpGet("Store/Category/{type}")]
    public async Task<IActionResult> Category(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            _logger.LogWarning("Category accessed without a type parameter.");
            return RedirectToAction(nameof(Articles));
        }

        // Try parsing the string to the Enum safely
        if (!Enum.TryParse(type, true, out PrintType categoryEnum))
        {
            _logger.LogWarning("Invalid category type requested: {Type}", type);
            return RedirectToAction(nameof(Articles));
        }

        var items = await _printService.GetByCategoryAsync(categoryEnum);

        ViewData["CategoryName"] = categoryEnum switch
        {
            PrintType.Figure => "Фигурки",
            PrintType.FidgetToy => "Играчки",
            PrintType.Accessory => "Аксесоари",
            _ => "Артикули"
        };

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(PrintsViewModel model)
    {
        if (model.Id == Guid.Empty) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        try
        {
            var existing = await _printService.GetByIdAsync(model.Id);
            if (existing == null) return NotFound();

            _logger.LogInformation("Admin is updating product: {PrintId}", model.Id);

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.Weight = model.Weight;
            existing.MaterialType = model.MaterialType;
            existing.PrintType = model.PrintType;
            existing.PrintColors = model.PrintColors;

            if (model.File != null)
            {
                _logger.LogDebug("Replacing image for Print {PrintId}", model.Id);
                await _fileService.DeleteImageAsync(existing.FilePath);
                existing.FilePath = await _fileService.UploadImageAsync(model.File, model.PrintType);
            }

            await _printService.UpdateAsync(existing);
            return RedirectToAction(nameof(Articles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {PrintId}", model.Id);
            return View(model);
        }
    }

    [HttpPost("Store/Delete/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var print = await _printService.GetByIdAsync(id);
        if (print == null) return NotFound();

        try
        {
            _logger.LogWarning("Admin initiated deletion of product: {PrintName} ({PrintId})", print.Name, id);

            await _cartService.RemoveAllByPrintIdAsync(id);
            await _wishlistService.RemoveAllByPrintIdAsync(id);

            if (!string.IsNullOrEmpty(print.FilePath) && !print.FilePath.Contains("placeholder"))
            {
                await _fileService.DeleteImageAsync(print.FilePath);
            }

            await _printService.DeleteAsync(print);
            _printService.ClearCache();

            _logger.LogInformation("Product {PrintId} successfully deleted and cache cleared.", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "CRITICAL: Database cleanup failed during deletion of Print {PrintId}!", id);
            return StatusCode(500, "Database cleanup failed.");
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        string? str = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(str, out userId);
    }
}