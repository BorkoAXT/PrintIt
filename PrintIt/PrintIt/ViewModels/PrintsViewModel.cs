using PrintIt.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PrintIt.ViewModels
{
    public class PrintsViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(100, ErrorMessage = "Името не може да бъде по-дълго от 100 символа.")]
        [Display(Name = "Име на продукта")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Описанието не може да бъде по-дълго от 1000 символа.")]
        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Моля, изберете материал.")]
        [Display(Name = "Материал")]
        public MaterialType MaterialType { get; set; }

        [Required(ErrorMessage = "Теглото е задължително.")]
        [Range(0.1, 10000, ErrorMessage = "Теглото трябва да бъде между 0.1 и 10,000 грама.")]
        [Display(Name = "Тегло (гр.)")]
        public double Weight { get; set; }

        [Required(ErrorMessage = "Цената е задължителна.")]
        [Range(0.01, 100000, ErrorMessage = "Цената трябва да бъде положително число.")]
        [Display(Name = "Цена (лв.)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Моля, изберете категория.")]
        [Display(Name = "Категория")]
        public PrintType PrintType { get; set; }

        [Required(ErrorMessage = "Изберете поне един цвят.")]
        [MinLength(1, ErrorMessage = "Трябва да изберете поне един цвят.")]
        public List<PrintColor> PrintColors { get; set; } = new();

        [Display(Name = "Наличен в магазина")]
        public bool IsAvailable { get; set; } = true;

        // This stores the actual filename after saving (e.g., "dragon.jpg")
        public string? FilePath { get; set; }

        [Required(ErrorMessage = "Моля, качете снимка на продукта.")]
        public IFormFile File { get; set; } = default!;
    }
}