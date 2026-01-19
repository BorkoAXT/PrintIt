namespace PrintIt.Models
{
    public class CartItem
    {

        /// <summary>
        /// Gets or sets the Guid of the Cart Item.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets pr sets the Guid of the Shopping Cart.
        /// </summary>
        public Guid ShopCartId { get; set; }
        
        /// <summary>
        /// Gets or sets the Shopping Cart.
        /// </summary>
        public ShopCart ShopCart { get; set; }

        /// <summary>
        /// Gets or sets the print's id.
        /// </summary>
        public Guid PrintId { get; set; }

        /// <summary>
        /// Gets or sets the Print.
        /// </summary>
        public Print Print { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the items in the shopping cart.
        /// </summary>
        public int Quantity { get; set; }
    }
}
