using Microsoft.AspNetCore.Identity;
using PrintIt.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace PrintIt.Models
{
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// The user's role (User, Admin)
        /// </summary>
        public Role Role { get; set; } = Role.User;

        /// <summary>
        /// Parameterless constructor for EF Core
        /// </summary>
        public User() : base()
        {
            Id = Guid.NewGuid();
        }
    }
}
