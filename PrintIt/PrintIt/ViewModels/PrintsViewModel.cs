using PrintIt.Models;
using System.ComponentModel.DataAnnotations;

namespace PrintIt.ViewModels
{
    public class PrintsViewModel : Print
    {
        [Required]
        public bool IsAvailable { get; set; } = true;

        [Required]
        public decimal Price { get; set; }

        public double ReviewScore { get; set; }

        [MaxLength(200)]
        public string Description { get; set;  } = string.Empty;

        [Required]

        public string ImageUrl => $"/images/{Id}.png";

    }
}
