using Microsoft.AspNetCore.Identity;

namespace PrintIt.Models
{
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// The user's shopping cart containing Prints
        /// </summary>
        public ShopCart? ShopCart { get; set; }

        /// <summary>
        /// Parameterless constructor for EF Core
        /// </summary>
        public User() : base() { }
    }
}
