using Common.Enums;
using Data;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;

namespace Services.Services
{
    public class FileService : IFileService
    {
        private static readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private static readonly string[] _modelExtensions = { ".stl", ".obj", ".step", ".stp" };
        
        // File size limits
        private const long MaxImageSize = 10 * 1024 * 1024; // 10 MB
        private const long Max3DModelSize = 100 * 1024 * 1024; // 100 MB

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public FileService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        /// <summary>
        /// Uploads an image to the legacy images folder (deprecated - disabled).
        /// </summary>
        public Task<string> UploadImageAsync(IFormFile file, PrintType type)
        {
            throw new NotSupportedException("Legacy UploadImageAsync is disabled. Use UploadPrintImageAsync.");
        }

        /// <summary>
        /// Uploads an image to database storage with sequence ordering.
        /// </summary>
        public async Task<string> UploadPrintImageAsync(IFormFile file, Guid printId, int sequenceNumber)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("Empty image file.");
            }

            // Validate file size
            if (file.Length > MaxImageSize)
            {
                throw new InvalidOperationException($"Image file is too large. Maximum size is {MaxImageSize / (1024 * 1024)} MB, but your file is {file.Length / (1024.0 * 1024.0):F1} MB.");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_imageExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"Image type {extension} is not supported. Allowed: {string.Join(", ", _imageExtensions)}");
            }

            var media = new PrintMedia
            {
                PrintId = printId,
                OriginalFileName = string.IsNullOrWhiteSpace(file.FileName) ? $"image{extension}" : file.FileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                Extension = extension,
                MediaType = MediaType.Image,
                SequenceNumber = sequenceNumber,
                Data = await ReadBytesAsync(file),
                CreatedOnUtc = DateTime.UtcNow
            };

            _db.PrintMedia.Add(media);
            await _db.SaveChangesAsync();

            return BuildMediaUrl(media);
        }

        /// <summary>
        /// Uploads a 3D model file to database storage.
        /// </summary>
        public async Task<string> Upload3DModelAsync(IFormFile file, Guid printId)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("Empty 3D model file.");
            }

            // Validate file size
            if (file.Length > Max3DModelSize)
            {
                throw new InvalidOperationException($"3D model file is too large. Maximum size is {Max3DModelSize / (1024 * 1024)} MB, but your file is {file.Length / (1024.0 * 1024.0):F1} MB.");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!_modelExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"File type {extension} is not supported. Allowed: {string.Join(", ", _modelExtensions)}");
            }

            var media = new PrintMedia
            {
                PrintId = printId,
                OriginalFileName = string.IsNullOrWhiteSpace(file.FileName) ? $"model{extension}" : file.FileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                Extension = extension,
                MediaType = MediaType.Model3D,
                SequenceNumber = 0,
                Data = await ReadBytesAsync(file),
                CreatedOnUtc = DateTime.UtcNow
            };

            _db.PrintMedia.Add(media);
            await _db.SaveChangesAsync();

            return BuildMediaUrl(media);
        }

        /// <summary>
        /// Gets all image files for a print, ordered by sequence number.
        /// </summary>
        public List<string> GetPrintImages(Guid printId)
        {
            return _db.PrintMedia
                .AsNoTracking()
                .Where(pm => pm.PrintId == printId && pm.MediaType == MediaType.Image)
                .OrderBy(pm => pm.SequenceNumber)
                .ThenBy(pm => pm.CreatedOnUtc)
                .ToList()
                .Select(BuildMediaUrl)
                .ToList();
        }

        /// <summary>
        /// Gets all 3D model files for a print.
        /// </summary>
        public List<string> GetPrint3DModels(Guid printId)
        {
            return _db.PrintMedia
                .AsNoTracking()
                .Where(pm => pm.PrintId == printId && pm.MediaType == MediaType.Model3D)
                .OrderBy(pm => pm.CreatedOnUtc)
                .ToList()
                .Select(BuildMediaUrl)
                .ToList();
        }

        /// <summary>
        /// Gets the first 3D model file (if exists).
        /// </summary>
        public string? GetPrint3DModel(Guid printId)
        {
            var models = GetPrint3DModels(printId);
            return models.FirstOrDefault();
        }

        /// <summary>
        /// Deletes a single media file from database storage.
        /// </summary>
        public async Task DeleteImageAsync(Guid mediaId)
        {
            try
            {
                var media = await _db.PrintMedia.FirstOrDefaultAsync(pm => pm.Id == mediaId);
                if (media == null)
                {
                    return;
                }

                _db.PrintMedia.Remove(media);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete media file {mediaId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes all media files for a print.
        /// </summary>
        public async Task DeletePrintMediaAsync(Guid printId)
        {
            try
            {
                var mediaItems = await _db.PrintMedia
                    .Where(pm => pm.PrintId == printId)
                    .ToListAsync();

                if (mediaItems.Count > 0)
                {
                    _db.PrintMedia.RemoveRange(mediaItems);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete media files for print {printId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Recalculates image sequence numbers after deletion.
        /// </summary>
        public async Task RecalculateImageSequenceAsync(Guid printId)
        {
            var images = await _db.PrintMedia
                .Where(pm => pm.PrintId == printId && pm.MediaType == MediaType.Image)
                .OrderBy(pm => pm.SequenceNumber)
                .ThenBy(pm => pm.CreatedOnUtc)
                .ToListAsync();

            int sequence = 1;
            foreach (var image in images)
            {
                image.SequenceNumber = sequence++;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Reorders images based on provided media IDs in order.
        /// </summary>
        public async Task ReorderImagesAsync(Guid printId, Guid[] orderedMediaIds)
        {
            var images = await _db.PrintMedia
                .Where(pm => pm.PrintId == printId && pm.MediaType == MediaType.Image)
                .ToListAsync();

            if (images.Count == 0)
            {
                return;
            }

            var orderedIdsList = orderedMediaIds.Distinct().ToList();
            int sequence = 1;

            foreach (var id in orderedIdsList)
            {
                var media = images.FirstOrDefault(i => i.Id == id);
                if (media != null)
                {
                    media.SequenceNumber = sequence++;
                }
            }

            foreach (var media in images.Where(i => !orderedIdsList.Contains(i.Id)).OrderBy(i => i.SequenceNumber).ThenBy(i => i.CreatedOnUtc))
            {
                media.SequenceNumber = sequence++;
            }

            await _db.SaveChangesAsync();
        }

        private static async Task<byte[]> ReadBytesAsync(IFormFile file)
        {
            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private static string BuildMediaUrl(PrintMedia media)
        {
            string fileName = string.IsNullOrWhiteSpace(media.OriginalFileName)
                ? $"file{media.Extension}"
                : media.OriginalFileName;

            return $"/media/{media.Id}/{Uri.EscapeDataString(fileName)}";
        }
    }
}