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
        /// Creates a virtual media path token for a print.
        /// </summary>
        public string CreatePrintMediaFolder(Guid printId, PrintType type)
        {
            string folder = type switch
            {
                PrintType.Figure => "Figures",
                PrintType.FidgetToy => "FidgetToys",
                PrintType.Accessory => "Accessories",
                _ => "Misc"
            };

            return $"/media/{folder}/{printId}";
        }

        /// <summary>
        /// Uploads an image to database storage with sequence ordering.
        /// </summary>
        public async Task<string> UploadPrintImageAsync(IFormFile file, string mediaFolderPath, int sequenceNumber)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("Empty image file.");
            }

            Guid printId = ParsePrintId(mediaFolderPath)
                ?? throw new InvalidOperationException("Invalid media folder path.");

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
        public async Task<string> Upload3DModelAsync(IFormFile file, string mediaFolderPath)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("Empty 3D model file.");
            }

            Guid printId = ParsePrintId(mediaFolderPath)
                ?? throw new InvalidOperationException("Invalid media folder path.");

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
        public List<string> GetPrintImages(string mediaFolderPath)
        {
            Guid? printId = ParsePrintId(mediaFolderPath);
            if (!printId.HasValue)
            {
                return new();
            }

            return _db.PrintMedia
                .AsNoTracking()
                .Where(pm => pm.PrintId == printId.Value && pm.MediaType == MediaType.Image)
                .OrderBy(pm => pm.SequenceNumber)
                .ThenBy(pm => pm.CreatedOnUtc)
                .ToList()
                .Select(BuildMediaUrl)
                .ToList();
        }

        /// <summary>
        /// Gets all 3D model files for a print.
        /// </summary>
        public List<string> GetPrint3DModels(string mediaFolderPath)
        {
            Guid? printId = ParsePrintId(mediaFolderPath);
            if (!printId.HasValue)
            {
                return new();
            }

            return _db.PrintMedia
                .AsNoTracking()
                .Where(pm => pm.PrintId == printId.Value && pm.MediaType == MediaType.Model3D)
                .OrderBy(pm => pm.CreatedOnUtc)
                .ToList()
                .Select(BuildMediaUrl)
                .ToList();
        }

        /// <summary>
        /// Gets the first 3D model file (if exists).
        /// </summary>
        public string? GetPrint3DModel(string mediaFolderPath)
        {
            var models = GetPrint3DModels(mediaFolderPath);
            return models.FirstOrDefault();
        }

        /// <summary>
        /// Deletes a single media file from database storage.
        /// </summary>
        public async Task DeleteImageAsync(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || filePath.Contains("placeholder"))
            {
                return;
            }

            Guid? mediaId = ParseMediaId(filePath);
            if (!mediaId.HasValue)
            {
                return;
            }

            var media = await _db.PrintMedia.FirstOrDefaultAsync(pm => pm.Id == mediaId.Value);
            if (media != null)
            {
                _db.PrintMedia.Remove(media);
                await _db.SaveChangesAsync();
            }

            _cache.Remove(filePath);
        }

        /// <summary>
        /// Deletes all media files for a print.
        /// </summary>
        public async Task DeletePrintMediaAsync(string mediaFolderPath)
        {
            Guid? printId = ParsePrintId(mediaFolderPath);
            if (!printId.HasValue)
            {
                return;
            }

            var mediaItems = await _db.PrintMedia
                .Where(pm => pm.PrintId == printId.Value)
                .ToListAsync();

            if (mediaItems.Count > 0)
            {
                _db.PrintMedia.RemoveRange(mediaItems);
                await _db.SaveChangesAsync();
            }

            _cache.Remove(mediaFolderPath);
        }

        /// <summary>
        /// Recalculates image sequence numbers after deletion.
        /// </summary>
        public async Task RecalculateImageSequenceAsync(string mediaFolderPath)
        {
            Guid? printId = ParsePrintId(mediaFolderPath);
            if (!printId.HasValue)
            {
                return;
            }

            var images = await _db.PrintMedia
                .Where(pm => pm.PrintId == printId.Value && pm.MediaType == MediaType.Image)
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
        /// Reorders images based on provided media URLs.
        /// </summary>
        public async Task ReorderImagesAsync(string mediaFolderPath, string[] orderedPaths)
        {
            Guid? printId = ParsePrintId(mediaFolderPath);
            if (!printId.HasValue)
            {
                return;
            }

            var images = await _db.PrintMedia
                .Where(pm => pm.PrintId == printId.Value && pm.MediaType == MediaType.Image)
                .ToListAsync();

            if (images.Count == 0)
            {
                return;
            }

            var orderedIds = orderedPaths
                .Select(ParseMediaId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            int sequence = 1;

            foreach (var id in orderedIds)
            {
                var media = images.FirstOrDefault(i => i.Id == id);
                if (media != null)
                {
                    media.SequenceNumber = sequence++;
                }
            }

            foreach (var media in images.Where(i => !orderedIds.Contains(i.Id)).OrderBy(i => i.SequenceNumber).ThenBy(i => i.CreatedOnUtc))
            {
                media.SequenceNumber = sequence++;
            }

            await _db.SaveChangesAsync();
        }

        private static Guid? ParsePrintId(string mediaFolderPath)
        {
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                return null;
            }

            string[] parts = mediaFolderPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return null;
            }

            return Guid.TryParse(parts[^1], out Guid printId) ? printId : null;
        }

        private static Guid? ParseMediaId(string mediaPath)
        {
            if (string.IsNullOrWhiteSpace(mediaPath))
            {
                return null;
            }

            string[] parts = mediaPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (int i = parts.Length - 1; i >= 0; i--)
            {
                string part = parts[i];

                if (Guid.TryParse(part, out Guid id))
                {
                    return id;
                }

                int dotIndex = part.IndexOf('.');
                if (dotIndex > 0 && Guid.TryParse(part[..dotIndex], out id))
                {
                    return id;
                }
            }

            return null;
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