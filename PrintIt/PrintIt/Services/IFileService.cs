using PrintIt.Enums;

namespace PrintIt.Services
{
    public interface IFileService
    {
        Task<string> UploadImageAsync(IFormFile file, PrintType type);
        Task DeleteImageAsync(string? filePath);
    }
}
