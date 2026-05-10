using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintIt.Enums;
using PrintIt.Models;
using PrintIt.ViewModels;
using Services.Interfaces;
using System.Security.Claims;

namespace Web.Controllers
{
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
        public async Task<IActionResult> Add3DPrint(PrintsViewModel model, IFormFileCollection? Images, IFormFileCollection? ModelFiles)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid for Add3DPrint: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors)));
                return View(model);
            }

            // Validate at least one image is provided
            if (Images == null || Images.Count == 0)
            {
                ModelState.AddModelError("Images", "Моля, добавете поне една снимка.");
                _logger.LogWarning("Add3DPrint attempted without images");
                return View(model);
            }

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

                // Upload all images in order
                int imageCount = 0;
                foreach (var imageFile in Images)
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            string imagePath = await _fileService.UploadPrintImageAsync(imageFile, mediaFolderPath, imageCount + 1);
                            _logger.LogInformation("Admin uploaded image {ImageCount} for Print {PrintId}: {ImagePath}", ++imageCount, print.Id, imagePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to upload image for Print {PrintId}", print.Id);
                        }
                    }
                }

                if (imageCount == 0)
                {
                    ModelState.AddModelError("Images", "Не беше възможно да качите дори една снимка. Моля проверете файловете.");
                    return View(model);
                }

                // Upload all 3D models if provided
                int modelCount = 0;
                if (ModelFiles != null && ModelFiles.Count > 0)
                {
                    foreach (var modelFile in ModelFiles)
                    {
                        if (modelFile != null && modelFile.Length > 0)
                        {
                            try
                            {
                                string modelPath = await _fileService.Upload3DModelAsync(modelFile, mediaFolderPath);
                                _logger.LogInformation("Admin uploaded 3D model {ModelCount} for Print {PrintId}: {ModelPath}", ++modelCount, print.Id, modelPath);
                            }
                            catch (InvalidOperationException ex)
                            {
                                _logger.LogWarning(ex, "Failed to upload 3D model for Print {PrintId}", print.Id);
                                ModelState.AddModelError("ModelFiles", ex.Message);
                                return View(model);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Unexpected error uploading 3D model for Print {PrintId}", print.Id);
                            }
                        }
                    }
                }

                await _printService.AddAsync(print);
                _logger.LogInformation("Admin created new product: {PrintName} ({PrintId}) with {ImageCount} images and {ModelCount} 3D models", print.Name, print.Id, imageCount, modelCount);

                return RedirectToAction(nameof(Articles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create new 3D print: {PrintName}", model.Name);
                ModelState.AddModelError(string.Empty, "Възникна грешка при публикуване на артикула. Моля попълнете формата отново.");
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

            // Load existing images and 3D models
            if (!string.IsNullOrEmpty(print.MediaFolderPath))
            {
                viewModel.ExistingImages = _fileService.GetPrintImages(print.MediaFolderPath);
                viewModel.Existing3DModel = _fileService.GetPrint3DModel(print.MediaFolderPath);
                // Load ALL 3D models
                viewModel.ExistingModels = _fileService.GetPrint3DModels(print.MediaFolderPath);
            }

            return View("~/Views/Store/Edit.cshtml", viewModel);
        }

        // POST: store/edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(PrintsViewModel model, IFormFileCollection? Images, IFormFileCollection? ModelFiles, string[]? RemovedImagePaths, string[]? RemovedModelPaths, string[]? ReorderedImagePaths)
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

                // Remove images if requested
                if (RemovedImagePaths != null && RemovedImagePaths.Length > 0)
                {
                    foreach (var imagePath in RemovedImagePaths)
                    {
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            try
                            {
                                await _fileService.DeleteImageAsync(imagePath);
                                _logger.LogInformation("Admin removed image from Print {PrintId}: {ImagePath}", model.Id, imagePath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete image {ImagePath} for Print {PrintId}", imagePath, model.Id);
                            }
                        }
                    }

                    // Recalculate sequence numbers after deletion
                    await _fileService.RecalculateImageSequenceAsync(existing.MediaFolderPath);
                    _logger.LogInformation("Recalculated image sequence numbers for Print {PrintId}", model.Id);
                }

                // Remove 3D models if requested
                if (RemovedModelPaths != null && RemovedModelPaths.Length > 0)
                {
                    foreach (var modelPath in RemovedModelPaths)
                    {
                        if (!string.IsNullOrEmpty(modelPath))
                        {
                            try
                            {
                                await _fileService.DeleteImageAsync(modelPath);
                                _logger.LogInformation("Admin removed 3D model from Print {PrintId}: {ModelPath}", model.Id, modelPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete 3D model {ModelPath} for Print {PrintId}", modelPath, model.Id);
                            }
                        }
                    }
                }

                // Handle image reordering if provided
                if (ReorderedImagePaths != null && ReorderedImagePaths.Length > 0)
                {
                    await _fileService.ReorderImagesAsync(existing.MediaFolderPath, ReorderedImagePaths);
                    _logger.LogInformation("Reordered images for Print {PrintId}", model.Id);
                }

                // Upload new images if provided
                if (Images != null && Images.Count > 0)
                {
                    var existingImages = _fileService.GetPrintImages(existing.MediaFolderPath);
                    int nextSequenceNumber = existingImages.Count + 1;
                    int uploadedCount = 0;

                    foreach (var imageFile in Images)
                    {
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            try
                            {
                                await _fileService.UploadPrintImageAsync(imageFile, existing.MediaFolderPath, nextSequenceNumber);
                                uploadedCount++;
                                nextSequenceNumber++;
                                _logger.LogDebug("Added new image to Print {PrintId}", model.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to upload image for Print {PrintId}", model.Id);
                            }
                        }
                    }

                    if (uploadedCount > 0)
                    {
                        _logger.LogInformation("Added {Count} images to Print {PrintId}", uploadedCount, model.Id);
                    }
                }

                // Upload new 3D models if provided
                int modelCount = 0;
                if (ModelFiles != null && ModelFiles.Count > 0)
                {
                    foreach (var modelFile in ModelFiles)
                    {
                        if (modelFile != null && modelFile.Length > 0)
                        {
                            try
                            {
                                string modelPath = await _fileService.Upload3DModelAsync(modelFile, existing.MediaFolderPath);
                                _logger.LogInformation("Admin uploaded 3D model for Print {PrintId}: {ModelPath}", model.Id, modelPath);
                                modelCount++;
                            }
                            catch (InvalidOperationException ex)
                            {
                                _logger.LogWarning(ex, "Failed to upload 3D model for Print {PrintId}", model.Id);
                                ModelState.AddModelError("ModelFiles", ex.Message);
                                return View(model);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Unexpected error uploading 3D model for Print {PrintId}", model.Id);
                            }
                        }
                    }
                }

                await _printService.UpdateAsync(existing);
                _logger.LogInformation("Product {PrintId} updated successfully", model.Id);

                return RedirectToAction(nameof(Articles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {PrintId}", model.Id);
                ModelState.AddModelError(string.Empty, "Възникна грешка при актуализиране на артикула.");
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
}