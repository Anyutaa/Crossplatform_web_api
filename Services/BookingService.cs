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

        // Получение всех броней (для админа — все, для пользователя — только свои)
        public async Task<List<Booking>> GetAllBookingsAsync(int currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return new List<Booking>();

            bool isAdmin = currentUser.Role == UserRole.Admin;

            if (isAdmin)
                return await _context.Bookings
                    .Include(b => b.BookingRooms)
                    .ToListAsync();

            return await _context.Bookings
                .Where(b => b.UserId == currentUserId)
                .Include(b => b.BookingRooms)
                .ToListAsync();
        }

        // Получить конкретную бронь
        public async Task<Booking?> GetBookingAsync(int id, int currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return null;

            bool isAdmin = currentUser.Role == UserRole.Admin;

            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return null;

            if (!isAdmin && booking.UserId != currentUserId)
                return null;

            return booking;
        }

        // Создание бронирования
        public async Task<(bool success, string? error)> CreateBookingAsync(int userId, int roomId, DateTime start, DateTime end)
        {
            var user = await _context.Users.FindAsync(userId);
            var room = await _context.Rooms.FindAsync(roomId);

            if (user == null)
                return (false, "Пользователь не найден.");

            if (user.Status != UserStatus.Active)
                return (false, "Заблокированный или архивированный пользователь не может создавать бронирования.");

            if (room == null)
                return (false, "Комната не найдена.");

            if (room.Status != RoomStatus.Available)
                return (false, "Комната недоступна для бронирования.");

            if (start >= end)
                return (false, "Дата окончания должна быть позже даты начала.");

            bool hasOverlap = await _context.Bookings
                .Include(b => b.BookingRooms)
                .AnyAsync(b =>
                    b.BookingRooms.Any(br => br.RoomId == roomId) &&
                    (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
                    b.StartDate < end &&
                    start < b.EndDate);

            if (hasOverlap)
                return (false, "Комната уже забронирована на эти даты.");

            // Создаём бронь
            var booking = new Booking
            {
                UserId = userId,
                StartDate = start,
                EndDate = end
            };

            var bookingRoom = new BookingRoom
            {
                RoomId = roomId,
                Booking = booking,
                PriceAtBooking = room.PricePerDay
            };

            booking.BookingRooms.Add(bookingRoom);
            booking.CalculateTotalPrice();

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        // Отмена брони
        public async Task<(bool success, string? error)> CancelBookingAsync(int id, int currentUserId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
                return (false, "Бронь не найдена.");

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return (false, "Пользователь не найден.");

            bool isAdmin = currentUser.Role == UserRole.Admin;
            bool isOwner = booking.UserId == currentUser.Id;

            if (!isAdmin && !isOwner)
                return (false, "Недостаточно прав для отмены этой брони.");

            booking.Cancel();
            await _context.SaveChangesAsync();
            return (true, null);
        }

        // Подтверждение брони (только админ)
        public async Task<(bool success, string? error)> ConfirmBookingAsync(int id, int currentUserId)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return (false, "Бронь не найдена.");

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return (false, "Пользователь не найден.");

            if (currentUser.Role != UserRole.Admin)
                return (false, "Только администратор может подтверждать брони.");

            try
            {
                booking.Confirm();
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
