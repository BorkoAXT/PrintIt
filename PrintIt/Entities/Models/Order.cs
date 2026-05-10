using System.ComponentModel.DataAnnotations;

namespace Entities.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerEmail { get; set; } = "";

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "EUR";

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        public string? PaymentProvider { get; set; }
    }
}