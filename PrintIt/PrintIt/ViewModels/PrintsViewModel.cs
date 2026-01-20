using PrintIt.Enums;
using System.ComponentModel.DataAnnotations;

namespace PrintIt.ViewModels
{
    /// <summary>
    /// View model used for creating and editing printable products.
    /// </summary>
    public class PrintsViewModel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the print.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the product.
        /// </summary>
        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(100, ErrorMessage = "Името не може да бъде по-дълго от 100 символа.")]
        [Display(Name = "Име на продукта")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the product.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Описанието не може да бъде по-дълго от 1000 символа.")]
        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the material used to produce the print.
        /// </summary>
        [Required(ErrorMessage = "Моля, изберете материал.")]
        [Display(Name = "Материал")]
        public MaterialType MaterialType { get; set; }

        /// <summary>
        /// Gets or sets the weight of the print in grams.
        /// </summary>
        [Required(ErrorMessage = "Теглото е задължително.")]
        [Range(0.1, 10000, ErrorMessage = "Теглото трябва да бъде между 0.1 и 10,000 грама.")]
        [Display(Name = "Тегло (гр.)")]
        public double Weight { get; set; }

        /// <summary>
        /// Gets or sets the price of the print.
        /// </summary>
        [Required(ErrorMessage = "Цената е задължителна.")]
        [Range(0.01, 100000, ErrorMessage = "Цената трябва да бъде положително число.")]
        [Display(Name = "Цена (лв.)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the category/type of the print.
        /// </summary>
        [Required(ErrorMessage = "Моля, изберете категория.")]
        [Display(Name = "Категория")]
        public PrintType PrintType { get; set; }

        /// <summary>
        /// Gets or sets the available colors for the print.
        /// </summary>
        [Required(ErrorMessage = "Изберете поне един цвят.")]
        [MinLength(1, ErrorMessage = "Трябва да изберете поне един цвят.")]
        [Display(Name = "Цветове")]
        public List<PrintColor> PrintColors { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the product is available in the store.
        /// </summary>
        [Display(Name = "Наличен в магазина")]
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Gets or sets the stored file path or file name of the product image.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the uploaded image file for the product.
        /// </summary>
        [Required(ErrorMessage = "Моля, качете снимка на продукта.")]
        [Display(Name = "Снимка на продукта")]
        public IFormFile File { get; set; } = default!;
    }
}
