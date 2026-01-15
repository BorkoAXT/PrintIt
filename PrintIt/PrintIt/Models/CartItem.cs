namespace PrintIt.Models
{
    public class CartItem
    {
        public Guid Id { get; set; }

        public Guid ShopCartId { get; set; }

        public ShopCart ShopCart { get; set; }

        public Guid PrintId { get; set; }

        public Print Print { get; set; }

        public int Quantity { get; set; }
    }
}
