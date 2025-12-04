using Microsoft.AspNetCore.Identity;

namespace PrintIt.Models
{
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// Parameterless constructor for EF Core
        /// </summary>
        public User() : base() { }
    }
}
