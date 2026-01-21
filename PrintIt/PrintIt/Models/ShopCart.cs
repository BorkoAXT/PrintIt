namespace PrintIt.Models
{
    /// <summary>
    /// Represents the official shopping cart used in the store.
    /// </summary>
    public class ShopCart
    {
        /// <summary>
        /// Gets or sets the id of the shop cart.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the user id from the shopping cart.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Gets the user from the shopping cart.
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// Gets or sets the items in the shopping cart of the user.
        /// </summary>
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

        protected ShopCart() { }

        public ShopCart(Guid userId)
        {
            Id = Guid.NewGuid();
            UserId = userId;
        }
    }
}
