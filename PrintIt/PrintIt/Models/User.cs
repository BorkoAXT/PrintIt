using Microsoft.AspNetCore.Identity;

namespace PrintIt.Models
{
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// Gets or sets the user's shop cart.
        /// </summary>
        public ShopCart? ShopCart { get; set; }

        /// <summary>
        /// Parameterless constructor for EF Core
        /// </summary>
        public User() : base() { }
    }
}
