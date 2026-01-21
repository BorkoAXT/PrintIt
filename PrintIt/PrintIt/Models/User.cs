using Microsoft.AspNetCore.Identity;

namespace PrintIt.Models
{
    /// <summary>
    /// Represents an application user.
    /// </summary>
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// Gets or sets the user's shop cart.
        /// </summary>
        public ShopCart? ShopCart { get; set; }

        /// <summary>
        /// Gets or sets the list of whitelisted prints for the user.
        /// </summary>
        public ICollection<UserWishlistItem> Wishlist { get; set; } = new List<UserWishlistItem>();

        /// <summary>
        /// Parameterless constructor for EF Core
        /// </summary>
        public User() : base() { }
    }
}
