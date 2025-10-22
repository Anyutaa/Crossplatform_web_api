using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crossplatform_2_smirnova.Models
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }

    public class Booking
    {
        public int Id { get; set; } 
        [Required]
        public int UserId { get; set; } 

        [Required]
        public BookingStatus Status { get; private set; } = BookingStatus.Pending; 
        [Required]
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow; 
        [Required]
        public DateTime StartDate { get; set; } 
        [Required]
        public DateTime EndDate { get; set; } 
        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; private set; }

        // Навигационное свойство для связи многие-ко-многим
        public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();

        // --- Бизнес-логика ---

        // Рассчитать цену брони (передаем стоимость комнаты)
        public void CalculateTotalPrice(decimal pricePerDay)
        {
            var days = (EndDate - StartDate).Days;
            if (days <= 0)
                throw new InvalidOperationException("EndDate must be after StartDate.");

            TotalPrice = days * pricePerDay;
        }

        // Отмена брони
        public void Cancel()
        {
            if (Status == BookingStatus.Cancelled)
                return;

            Status = BookingStatus.Cancelled;
        }

        // Подтверждение брони
        public void Confirm()
        {
            if (Status != BookingStatus.Pending)
                throw new InvalidOperationException("Only pending bookings can be confirmed.");

            Status = BookingStatus.Confirmed;
        }
    }
}
