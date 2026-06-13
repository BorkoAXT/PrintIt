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
        /// Uploads an image to a print with sequence ordering. Stored in database.
        /// </summary>
        Task<string> UploadPrintImageAsync(IFormFile file, Guid printId, int sequenceNumber);

        /// <summary>
        /// Uploads a 3D model file to a print. Stored in database.
        /// </summary>
        Task<string> Upload3DModelAsync(IFormFile file, Guid printId);

        /// <summary>
        /// Gets all image URLs for a print.
        /// </summary>
        List<string> GetPrintImages(Guid printId);

        /// <summary>
        /// Gets all 3D model URLs for a print.
        /// </summary>
        List<string> GetPrint3DModels(Guid printId);

        /// <summary>
        /// Gets the first 3D model URL for a print (if exists).
        /// </summary>
        string? GetPrint3DModel(Guid printId);

        /// <summary>
        /// Deletes a single media file by ID.
        /// </summary>
        Task DeleteImageAsync(Guid mediaId);

        /// <summary>
        /// Deletes all media files for a print.
        /// </summary>
        Task DeletePrintMediaAsync(Guid printId);

        /// <summary>
        /// Recalculates sequence numbers for images after deletion.
        /// </summary>
        Task RecalculateImageSequenceAsync(Guid printId);

        /// <summary>
        /// Reorders images based on provided media IDs in order.
        /// </summary>
        Task ReorderImagesAsync(Guid printId, Guid[] orderedMediaIds);
    }
}