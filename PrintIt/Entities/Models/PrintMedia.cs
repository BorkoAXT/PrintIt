using Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Entities.Models
{
    public class PrintMedia
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PrintId { get; set; }

        public Print Print { get; set; } = null!;

        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Extension { get; set; } = string.Empty;

        public MediaType MediaType { get; set; }

        public int SequenceNumber { get; set; }

        public byte[] Data { get; set; } = Array.Empty<byte>();

        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
