using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.Services;
using PrintIt.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Security.Claims;

public class StoreController : Controller
{
    private readonly IPrintService _printService;
    private readonly IFileService _fileService;
    private readonly IWishlistService _wishlistService;
    private readonly ICartService _cartService;

    public StoreController(
        IPrintService printService,
        IFileService fileService,
        IWishlistService wishlistService, ICartService cartService)
    {
        _printService = printService;
        _fileService = fileService;
        _wishlistService = wishlistService;
        _cartService = cartService;
    }

    [HttpGet("Store")]
    public async Task<IActionResult> Articles(StoreSearchViewModel filter)
    {
        bool hasFilters = Request.Query.Keys.Any(k => k != "handler");

        if (hasFilters)
        {
            filter.Results = await _printService.SearchAsync(filter);
            ViewData["IsFiltering"] = true;
        }
        else
        {
            var allItems = await _printService.GetAllAsync(); // Cached
            filter.RecentFigures = allItems.Where(p => p.PrintType == PrintType.Figure).Take(4).ToList();
            filter.RecentFidgetToys = allItems.Where(p => p.PrintType == PrintType.FidgetToy).Take(4).ToList();
            filter.RecentAccessories = allItems.Where(p => p.PrintType == PrintType.Accessory).Take(4).ToList();
            ViewData["IsFiltering"] = false;
        }

        return View(filter);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var print = await _printService.GetByIdAsync(id); // Cached
        if (print == null) return NotFound();

        bool isFavorite = false;
        if (TryGetUserId(out Guid userId))
            isFavorite = await _wishlistService.IsFavoriteAsync(userId, id);

        ViewBag.IsFavorite = isFavorite;
        return View(print);
    }

    [HttpGet("Store/Category")]
    public async Task<IActionResult> Category(string type)
    {
        if (string.IsNullOrEmpty(type))
            return RedirectToAction(nameof(Articles));

        if (!Enum.TryParse(type, true, out PrintType categoryEnum))
            return RedirectToAction(nameof(Articles));

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
    public async Task<IActionResult> ToggleWishlist(Guid id)
    {
        if (!TryGetUserId(out Guid userId))
            return Json(new { success = false, message = "Unauthorized" });

        bool isFavorite = await _wishlistService.ToggleAsync(userId, id);
        return Json(new { success = true, isFavorite });
    }

    [HttpGet("Store/Add")]
    [Authorize(Roles = "Admin")]
    public IActionResult Add3DPrint()
    {
        return View("~/Views/Store/Add3DPrint.cshtml", new PrintsViewModel());
    }

    // Admin Add/Edit/Delete operations automatically clear cache inside PrintService
    [HttpPost("Store/Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add3DPrint(PrintsViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        string filePath = "/images/placeholder-figure.jpg";
        if (model.File != null)
            filePath = await _fileService.UploadImageAsync(model.File, model.PrintType);

        var print = new Print(
            model.Name,
            model.Description,
            model.MaterialType,
            model.Weight,
            model.Price,
            filePath,
            model.PrintType,
            model.PrintColors
        );

        await _printService.AddAsync(print);
        return RedirectToAction(nameof(Articles));
    }

    [HttpGet("Store/Edit/{id}")]
    [Authorize(Roles = "Admin")]

    public async Task<IActionResult> Edit(Guid Id)
    {
        var print = await _printService.GetByIdAsync(Id);
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
            FilePath = print.FilePath
        };
        return View(viewModel);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(PrintsViewModel model)
    {
        if (model.Id == Guid.Empty) return BadRequest();

        if (!ModelState.IsValid) return View(model);

        var existing = await _printService.GetByIdAsync(model.Id);
        if (existing == null) return NotFound();

        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.Price = model.Price;
        existing.Weight = model.Weight;
        existing.MaterialType = model.MaterialType;
        existing.PrintType = model.PrintType;
        existing.PrintColors = model.PrintColors;

        if (model.File != null)
        {
            await _fileService.DeleteImageAsync(existing.FilePath);
            existing.FilePath = await _fileService.UploadImageAsync(model.File, model.PrintType);
        }

        await _printService.UpdateAsync(existing);

        return RedirectToAction(nameof(Articles));
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
            await _cartService.RemoveAllByPrintIdAsync(id);
            await _wishlistService.RemoveAllByPrintIdAsync(id);

            if (!string.IsNullOrEmpty(print.FilePath))
            {
                await _fileService.DeleteImageAsync(print.FilePath);
            }

            await _printService.DeleteAsync(print);

            _printService.ClearCache();

            return Ok();
        }
        catch (Exception ex)
        {
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