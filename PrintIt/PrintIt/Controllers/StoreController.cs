using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.Services;
using PrintIt.ViewModels;
using System;
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
            // Create a new print with temporary media folder path
            var print = new Print(
                model.Name, model.Description, model.MaterialType, model.Weight,
                model.Price, null, model.PrintType, model.PrintColors
            );

            // Create the media folder for this print
            string mediaFolderPath = _fileService.CreatePrintMediaFolder(print.Id, model.PrintType);
            print.MediaFolderPath = mediaFolderPath;

            _logger.LogInformation("Created media folder for Print {PrintId}: {MediaFolderPath}", print.Id, mediaFolderPath);

            // Upload image if provided
            if (model.File != null)
            {
                string imagePath = await _fileService.UploadPrintImageAsync(model.File, mediaFolderPath);
                _logger.LogInformation("Admin uploaded image for Print {PrintId}: {ImagePath}", print.Id, imagePath);
            }

            // Upload 3D model if provided
            if (model.ModelFile != null)
            {
                try
                {
                    string modelPath = await _fileService.Upload3DModelAsync(model.ModelFile, mediaFolderPath);
                    _logger.LogInformation("Admin uploaded 3D model for Print {PrintId}: {ModelPath}", print.Id, modelPath);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Failed to upload 3D model for Print {PrintId}", print.Id);
                    ModelState.AddModelError("ModelFile", ex.Message);
                    return View(model);
                }
            }

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

    // GET: store/edit/{id}
    [HttpGet("Store/Edit/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var print = await _printService.GetByIdAsync(id);
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
            PrintColors = print.PrintColors ?? new List<PrintColor>(),
            MediaFolderPath = print.MediaFolderPath
        };

        // Load existing images and 3D model
        if (!string.IsNullOrEmpty(print.MediaFolderPath))
        {
            viewModel.ExistingImages = _fileService.GetPrintImages(print.MediaFolderPath);
            viewModel.Existing3DModel = _fileService.GetPrint3DModel(print.MediaFolderPath);
        }

        return View("~/Views/Store/Edit.cshtml", viewModel);
    }

    // POST: store/edit/{id}
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

            // Ensure media folder exists
            if (string.IsNullOrEmpty(existing.MediaFolderPath))
            {
                existing.MediaFolderPath = _fileService.CreatePrintMediaFolder(existing.Id, model.PrintType);
                _logger.LogInformation("Created missing media folder for Print {PrintId}", existing.Id);
            }

            // Upload new image if provided
            if (model.File != null)
            {
                _logger.LogDebug("Adding new image for Print {PrintId}", model.Id);
                await _fileService.UploadPrintImageAsync(model.File, existing.MediaFolderPath);
            }

            // Upload new 3D model if provided
            if (model.ModelFile != null)
            {
                try
                {
                    _logger.LogDebug("Uploading new 3D model for Print {PrintId}", model.Id);
                    await _fileService.Upload3DModelAsync(model.ModelFile, existing.MediaFolderPath);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Failed to upload 3D model for Print {PrintId}", model.Id);
                    ModelState.AddModelError("ModelFile", ex.Message);
                    return View(model);
                }
            }

            // Remove image if requested
            if (!string.IsNullOrEmpty(model.RemoveImagePath))
            {
                await _fileService.DeleteImageAsync(model.RemoveImagePath);
                _logger.LogInformation("Admin removed image from Print {PrintId}: {ImagePath}", model.Id, model.RemoveImagePath);
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

            // Delete entire media folder and its contents
            if (!string.IsNullOrEmpty(print.MediaFolderPath))
            {
                await _fileService.DeletePrintMediaAsync(print.MediaFolderPath);
                _logger.LogInformation("Deleted media folder for Print {PrintId}: {MediaFolderPath}", id, print.MediaFolderPath);
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