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

        public async Task DeleteImageAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || filePath.Contains("placeholder")) return;

            string fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            _cache.Remove(filePath);
        }
    }
}