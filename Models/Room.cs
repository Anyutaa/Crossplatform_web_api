using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Crossplatform_2_smirnova.Data;

namespace Crossplatform_2_smirnova.Models
{
    public enum RoomStatus
    {
        Available,
        Maintenance,
        Blocked,
        Archived
    }

    public class Room
    {

        public int Id { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal PricePerDay { get; set; }

        [Required]
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();

        public void Archive() => Status = RoomStatus.Archived;
        public void SetMaintenance() => Status = RoomStatus.Maintenance;
        public void SetBlocked() => Status = RoomStatus.Blocked;
        public void Restore() => Status = RoomStatus.Available;

        public bool CanBeBooked() => Status == RoomStatus.Available;

        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice < 0)
                throw new ArgumentException("Price cannot be negative");

            PricePerDay = newPrice;
        }
    }
}
