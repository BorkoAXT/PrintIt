using PrintIt.Enums;

namespace PrintIt.Models
{
    /// <summary>
    /// Represents the printable item in the store.
    /// </summary>
    public class Print
    {
        /// <summary>
        /// Gets or sets the unique identifier of the print.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the date and time when the print was added to the catalog.
        /// </summary>
        public DateTime AddedOn { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the name of the print.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the print.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the material used to produce the print.
        /// </summary>
        public MaterialType MaterialType { get; set; }

        /// <summary>
        /// Gets or sets the weight of the print (in grams, if that's your unit).
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Gets or sets the price of the print.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the folder path containing all print images and 3D files.
        /// </summary>
        public string? MediaFolderPath { get; set; }

        /// <summary>
        /// Gets or sets the type of the print.
        /// </summary>
        public PrintType PrintType { get; set; }

        /// <summary>
        /// Gets or sets the available colors for the print.
        /// </summary>
        public List<PrintColor> PrintColors { get; set; } = new();

        /// <summary>
        /// Parameterless constructor required by EF Core.
        /// </summary>
        protected Print() { }

        /// <summary>
        /// Creates a new <see cref="Print"/>.
        /// </summary>
        public Print(
            string name,
            string description,
            MaterialType materialType,
            double weight,
            decimal price,
            string? mediaFolderPath,
            PrintType printType,
            List<PrintColor> colors)
        {
            Id = Guid.NewGuid();
            AddedOn = DateTime.UtcNow;
            Name = name;
            Description = description;
            MaterialType = materialType;
            Weight = weight;
            Price = price;
            MediaFolderPath = mediaFolderPath;
            PrintType = printType;
            PrintColors = colors ?? new() { PrintColor.None };
        }
    }
}
