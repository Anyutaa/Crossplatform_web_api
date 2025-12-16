using Crossplatform_2_smirnova.Models;
namespace Crossplatform_2_smirnova.DTOs.Bookings
{
    public class BookingDto
    {
        public int Id { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }

        public List<BookingRoomDto> Rooms { get; set; } = new();
    }
}
