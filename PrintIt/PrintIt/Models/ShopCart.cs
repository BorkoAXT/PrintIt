namespace PrintIt.Models
{
    public class ShopCart
    {
        /// <summary>
        /// ShopCart Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// UserId of the owner of the ShopCart
        /// </summary>
        public Guid UserId { get; set; }
        public User User { get; set; }

        /// <summary>
        /// Shopping cart containing Prints
        /// </summary>
        public List<CartItem> Items { get; set; } = new();
    }
}
