using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Crossplatform_2_smirnova.Models
{
    public class BookingRoom
    {
        [Required]
        public int BookingId { get; set; }

        [JsonIgnore]
        public Booking Booking { get; set; } = null!;

        [Required]
        public int RoomId { get; set; }

        [JsonIgnore]
        public Room Room { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal PriceAtBooking { get; set; }
    }
}
