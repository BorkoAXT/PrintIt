using Microsoft.Extensions.Caching.Memory;
using PrintIt.Enums;

namespace PrintIt.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        public FileService(IWebHostEnvironment env, IMemoryCache cache)
        {
            _env = env;
            _cache = cache;
        }

        /// <summary>
        /// Uploads an image to the print's media folder.
        /// </summary>
        public async Task<string> UploadImageAsync(IFormFile file, PrintType type)
        {
            string folder = type switch
            {
                PrintType.Figure => "Figures",
                PrintType.FidgetToy => "FidgetToys",
                PrintType.Accessory => "Accessories",
                _ => "Misc"
            };

            string uploads = Path.Combine(_env.WebRootPath, "images", folder);
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            string fileName = Guid.NewGuid() + "_" + file.FileName;
            string fullPath = Path.Combine(uploads, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/{folder}/{fileName}";
        }

        /// <summary>
        /// Creates a media folder for a print and returns its path.
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

            string mediaPath = Path.Combine(_env.WebRootPath, "media", folder, printId.ToString());
            if (!Directory.Exists(mediaPath))
                Directory.CreateDirectory(mediaPath);

            return $"/media/{folder}/{printId}";
        }

        /// <summary>
        /// Uploads an image to a print's media folder.
        /// </summary>
        public async Task<string> UploadPrintImageAsync(IFormFile file, string mediaFolderPath)
        {
            string fullPath = Path.Combine(_env.WebRootPath, mediaFolderPath.TrimStart('/'));
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            string fileName = Guid.NewGuid() + "_" + file.FileName;
            string filePath = Path.Combine(fullPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"{mediaFolderPath}/{fileName}";
        }

        /// <summary>
        /// Uploads a 3D model file to a print's media folder.
        /// </summary>
        public async Task<string> Upload3DModelAsync(IFormFile file, string mediaFolderPath)
        {
            // Validate file extension (e.g., .stl, .obj, .3mf)
            string[] allowedExtensions = { ".stl", ".obj", ".3mf", ".gcode" };
            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new InvalidOperationException($"File type {fileExtension} is not supported. Allowed: {string.Join(", ", allowedExtensions)}");

            string fullPath = Path.Combine(_env.WebRootPath, mediaFolderPath.TrimStart('/'));
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            string fileName = Guid.NewGuid() + "_model" + fileExtension;
            string filePath = Path.Combine(fullPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"{mediaFolderPath}/{fileName}";
        }

        /// <summary>
        /// Gets all image files in a print's media folder.
        /// </summary>
        public List<string> GetPrintImages(string mediaFolderPath)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            string fullPath = Path.Combine(_env.WebRootPath, mediaFolderPath.TrimStart('/'));

            if (!Directory.Exists(fullPath))
                return new();

            return Directory
                .GetFiles(fullPath)
                .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => $"{mediaFolderPath}/{Path.GetFileName(f)}")
                .ToList();
        }

        /// <summary>
        /// Gets the 3D model file in a print's media folder (if exists).
        /// </summary>
        public string? GetPrint3DModel(string mediaFolderPath)
        {
            string[] modelExtensions = { ".stl", ".obj", ".3mf", ".gcode" };
            string fullPath = Path.Combine(_env.WebRootPath, mediaFolderPath.TrimStart('/'));

            if (!Directory.Exists(fullPath))
                return null;

            var modelFile = Directory
                .GetFiles(fullPath)
                .FirstOrDefault(f => modelExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

            return modelFile != null ? $"{mediaFolderPath}/{Path.GetFileName(modelFile)}" : null;
        }

        /// <summary>
        /// Deletes an image file from the server and removes it from cache.
        /// </summary>
        /// <param name="filePath">The relative path of the image file to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteImageAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || filePath.Contains("placeholder")) return;

            string fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            _cache.Remove(filePath);
        }

        /// <summary>
        /// Deletes an entire print's media folder and all its contents.
        /// </summary>
        public async Task DeletePrintMediaAsync(string mediaFolderPath)
        {
            if (string.IsNullOrEmpty(mediaFolderPath)) return;

            string fullPath = Path.Combine(_env.WebRootPath, mediaFolderPath.TrimStart('/'));
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive: true);
                _cache.Remove(mediaFolderPath);
            }

            await Task.CompletedTask;
        }
    }
}