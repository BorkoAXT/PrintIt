using PrintIt.Models;
using System.ComponentModel.DataAnnotations.Schema;


namespace Entities.Models
{
    /// <summary>
    /// Represents an item in a shopping cart.
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// Gets or sets the unique identifier of the cart item.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the unique identifier of the shopping cart.
        /// </summary>
        public Guid ShopCartId { get; set; }

        /// <summary>
        /// Gets or sets the shopping cart this item belongs to.
        /// </summary>
        public ShopCart? ShopCart { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the print.
        /// </summary>
        public Guid PrintId { get; set; }

        /// <summary>
        /// Gets or sets the print associated with this cart item.
        /// </summary>
        public Print? Print { get; set; }

        /// <summary>
        /// Gets or sets the quantity of this item in the shopping cart.
        /// </summary>
        public int Quantity { get; set; }

        public string ColorString { get; set; } = "";

        [NotMapped]
        public List<PrintColor> Colours
        {
            get => string.IsNullOrEmpty(ColorString)
                ? new List<PrintColor>()
                : ColorString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Enum.Parse<PrintColor>(s))
                    .ToList();
            set => ColorString = value != null
                ? string.Join(',', value.OrderBy(c => c))
                : "";
        }

        public string UniqueKey { get; set; } = string.Empty;

        public CartItem(Guid shopCartId, Guid printId)
        {
            Id = Guid.NewGuid();
            ShopCartId = shopCartId;
            PrintId = printId;
        }
    }
}