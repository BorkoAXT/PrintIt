using System.ComponentModel.DataAnnotations;

namespace PrintIt.ViewModels
{
    public class RegisterViewModel
    {
        /// <summary>
        /// Gets or sets the username chosen by the user.
        /// Must be between 6 and 64 characters and contain only letters, numbers, and underscores.
        /// </summary>
        [Required]
        [MinLength(6)]
        [MaxLength(64)]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// Must be a valid email format and no longer than 64 characters.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(64)]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the user's password.
        /// Must be between 6 and 128 characters.
        /// </summary>
        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number.
        /// Must be a valid phone number and no longer than 12 characters.
        /// </summary>
        [Required]
        [Phone]
        [MaxLength(12)]
        public string? PhoneNumber { get; set; }
    }
}