using System.ComponentModel.DataAnnotations;

namespace PrintIt.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [MinLength(6)]
        [MaxLength(64)]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
        public string? Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(64)]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string? Password { get; set; }

        [Required]
        [Phone]
        [MaxLength(12)]
        public string? PhoneNumber { get; set; }
    }
}
