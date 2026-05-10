namespace Entities.Models
{
    public class UserWishlistItem
    {
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;

        public Guid PrintId { get; private set; }
        public Print Print { get; private set; } = null!;

        public DateTime CreatedAt { get; private set; }

        protected UserWishlistItem() { }

        public UserWishlistItem(Guid userId, Guid printId)
        {
            UserId = userId;
            PrintId = printId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
