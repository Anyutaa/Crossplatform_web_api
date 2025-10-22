using Microsoft.EntityFrameworkCore;
using Crossplatform_2_smirnova.Data;
using Crossplatform_2_smirnova.Models;

namespace Crossplatform_2_smirnova.Services
{
    public class BookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoomService _roomService;

        public BookingService(ApplicationDbContext context, RoomService roomService)
        {
            _context = context;
            _roomService = roomService;
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            return await _context.Bookings.ToListAsync();
        }

        public async Task<Booking?> GetBookingAsync(int id)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<(bool success, string? error)> CreateBookingAsync(int userId, int roomId, DateTime start, DateTime end)
        {

            var user = await _context.Users.FindAsync(userId);
            var room = await _context.Rooms.FindAsync(roomId);

            if (user == null)
                return (false, "Пользователь не найден.");

            if (!user.IsActive)
                return (false, "Пользователь заблокирован и не может создавать бронирования.");

            if (room == null)
                return (false, "Комната не найдена.");

            if (start >= end)
                return (false, "Дата окончания должна быть позже даты начала.");

            //if (!await _roomService.CheckAvailabilityAsync(roomId, start, end))
            //    return (false, "Комната недоступна на эти даты.");

            // Создание брони
            var booking = new Booking
            {
                UserId = userId,
                StartDate = start,
                EndDate = end
            };

            booking.CalculateTotalPrice(room.PricePerDay);
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool success, string? error)> CancelBookingAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return (false, "Бронь не найдена.");

            booking.Cancel();
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string? error)> ConfirmBookingAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return (false, "Бронь не найдена.");

            booking.Confirm();
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
