using PrintIt.Enums;

namespace PrintIt.Models
{
    public class Print
    {

        /// <summary>
        /// The Print's Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the Print
        /// </summary>
        public string? Name { get; set; } = string.Empty;


        /// <summary>
        /// The material type used to make the Print
        /// </summary>
        public MaterialType MaterialType { get; set; } = MaterialType.PLA;

        /// <summary>
        /// The weight of the Print
        /// </summary>
        public int Weight { get; set; }




    }
}
