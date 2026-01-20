using PrintIt.Enums;
using PrintIt.Helpers;

namespace PrintIt.Models
{
    public class Print
    {

        /// <summary>
        /// Gets or sets the id of the print.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the print.
        /// </summary>
        public string? Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the print.
        /// </summary>
        public string? Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the material used to make the print.
        /// </summary>
        public string MaterialType { get; set; }

        /// <summary>
        /// Gets or sets the weight of the print.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Gets or sets the price of the print.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the file path of the print.
        /// </summary>
        public string? FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the print.
        /// </summary>
        public string PrintType { get; set; }

        /// <summary>
        /// Gets or sets the color of the print.
        /// </summary>
        public List<PrintColor> PrintColors { get; set; } = new List<PrintColor> { PrintColor.None };

        private Print() { }

        public Print(string name, string description, MaterialType materialType, double weight, decimal price, string? FilePath, PrintType printType, List<PrintColor> colors)
        {
            this.Id = Guid.NewGuid();
            this.Name = name;
            this.Description = description;
            this.MaterialType = EnumToStringHelper.GetEnumNameText<MaterialType>();
            this.Weight = weight;
            this.Price = price;
            this.FilePath = FilePath;
            this.PrintType = EnumToStringHelper.GetEnumNameText<PrintType>();
            this.PrintColors = colors;
        }
    }
}
