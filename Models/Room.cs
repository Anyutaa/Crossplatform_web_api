using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Crossplatform_2_smirnova.Data;

namespace Crossplatform_2_smirnova.Models
{
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

        public bool IsActive { get; set; } = true;

        // Навигационное свойство для связи многие-ко-многим
        public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();

        // --- Бизнес-логика ---

        public void MarkAsDeleted() => IsActive = false;
        public void Restore() => IsActive = true;

        public bool CanBeBooked() => IsActive;

        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice < 0)
                throw new ArgumentException("Price cannot be negative");

            PricePerDay = newPrice;
        }
    }
}
