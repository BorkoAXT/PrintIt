using PrintIt.Enums;

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
        public MaterialType MaterialType { get; set; } = MaterialType.PLA;

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
    }
}
