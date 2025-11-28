using PrintIt.Models;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

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
        public Image? Image => LoadImage();

        private Image LoadImage()
        {
            string _imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", $"{Id}.png");
            if (File.Exists(_imagePath))
            {
                return Image.FromFile(_imagePath);
            }
            return null;
        }

    }
}
