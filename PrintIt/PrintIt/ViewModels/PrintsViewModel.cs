using PrintIt.Models;
using System.ComponentModel.DataAnnotations;

namespace PrintIt.ViewModels
{
    public class PrintsViewModel : Print
    {
        /// <summary>
        /// Indicates whether the print is available or not.
        /// </summary>
        
        [Required]
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Gets or sets the price of the print.
        /// </summary>

        [Required]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the average star rating of the print.
        /// </summary>
        public double ReviewScore { get; set; }

        /// <summary>
        /// Gets or sets a short description of the print.
        /// </summary>

        [MaxLength(200)]
        public string Description { get; set;  } = string.Empty;

        /// <summary>
        /// Gets the relative URL of the image associated with the print.
        /// </summary>

        [Required]

        public string ImageUrl => $"/images/{Id}.png";

        /// <summary>
        /// Gets or sets the uploaded file for the print's image. 
        /// </summary>
        
        [Required]
        public IFormFile File { get; set; }

    }
}
