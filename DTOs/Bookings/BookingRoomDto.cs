using Crossplatform_2_smirnova.Models;

namespace Crossplatform_2_smirnova.DTOs.Bookings
{
    public class BookingRoomDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public decimal PriceAtBooking { get; set; }

    }
}
