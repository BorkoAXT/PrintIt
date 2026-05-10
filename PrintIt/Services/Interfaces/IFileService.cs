using Common.Enums;
using Microsoft.AspNetCore.Http;

namespace Services.Interfaces
{
    public interface IFileService
    {
        /// <summary>
        /// Uploads an image to the legacy images folder (deprecated - use UploadPrintImageAsync instead).
        /// </summary>
        Task<string> UploadImageAsync(IFormFile file, PrintType type);

        /// <summary>
        /// Creates a media folder for a print and returns its path.
        /// </summary>
        string CreatePrintMediaFolder(Guid printId, PrintType type);

        /// <summary>
        /// Uploads an image to a print's media folder with sequence ordering.
        /// </summary>
        Task<string> UploadPrintImageAsync(IFormFile file, string mediaFolderPath, int sequenceNumber);

        /// <summary>
        /// Uploads a 3D model file to a print's media folder.
        /// </summary>
        Task<string> Upload3DModelAsync(IFormFile file, string mediaFolderPath);

        /// <summary>
        /// Gets all image files in a print's media folder.
        /// </summary>
        List<string> GetPrintImages(string mediaFolderPath);

        /// <summary>
        /// Gets all 3D model files in a print's media folder.
        /// </summary>
        List<string> GetPrint3DModels(string mediaFolderPath);

        /// <summary>
        /// Gets the first 3D model file in a print's media folder (if exists).
        /// </summary>
        string? GetPrint3DModel(string mediaFolderPath);

        /// <summary>
        /// Deletes a single image file.
        /// </summary>
        Task DeleteImageAsync(string? filePath);

        /// <summary>
        /// Deletes an entire print's media folder and all its contents.
        /// </summary>
        Task DeletePrintMediaAsync(string mediaFolderPath);

        /// <summary>
        /// Recalculates sequence numbers for images after deletion.
        /// </summary>
        Task RecalculateImageSequenceAsync(string mediaFolderPath);

        /// <summary>
        /// Reorders images based on provided file paths.
        /// </summary>
        Task ReorderImagesAsync(string mediaFolderPath, string[] orderedPaths);
    }
}