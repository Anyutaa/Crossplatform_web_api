using Crossplatform_2_smirnova.Data;
using Crossplatform_2_smirnova.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Crossplatform_2_smirnova.DTOs.Bookings;

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
        public async Task<List<BookingDto>> GetAllBookingsAsync(int currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);

            var bookingsQuery = _context.Bookings
                .Include(b => b.BookingRooms)
                .ThenInclude(br => br.Room)
                .AsQueryable();

            if (currentUser?.Role != UserRole.Admin)
                bookingsQuery = bookingsQuery.Where(b => b.UserId == currentUserId);

            var bookings = await bookingsQuery.ToListAsync();

            return bookings.Select(b => new BookingDto
            {
                Id = b.Id,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TotalPrice = b.TotalPrice,
                Rooms = b.BookingRooms.Select(br => new BookingRoomDto
                {
                    RoomId = br.RoomId,
                    RoomName = br.Room.Name, // вот здесь берём имя из Room
                    PriceAtBooking = br.PriceAtBooking
                }).ToList()
            }).ToList();
        }

        // Получить конкретную бронь
        public async Task<Booking?> GetBookingAsync(int id, int currentUserId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return null;

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser?.Role != UserRole.Admin && booking.UserId != currentUserId)
                return null;

            return booking;
        }

        // Создание бронирования
        public async Task<(bool success, string? error)> CreateBookingAsync(int userId, [FromQuery] string roomIds, DateTime start, DateTime end)
        {
            var roomIdArray = roomIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(int.Parse)
                                 .ToArray();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return (false, "Пользователь не найден.");

            if (user.Status != UserStatus.Active)
                return (false, "Заблокированный или архивированный пользователь не может создавать бронирования.");

            if (roomIdArray == null || roomIdArray.Length == 0)
                return (false, "Не указаны комнаты для бронирования.");

            if (start >= end)
                return (false, "Дата окончания должна быть позже даты начала.");

            // Получаем все комнаты одним запросом
            var rooms = await _context.Rooms
                .Where(r => roomIdArray.Contains(r.Id))
                .ToListAsync();

            if (rooms.Count != roomIdArray.Length)
                return (false, "Одна или несколько комнат не найдены.");

            // Проверяем доступность всех комнат
            var unavailableRoom = rooms.FirstOrDefault(r => r.Status != RoomStatus.Available);
            if (unavailableRoom != null)
                return (false, $"Комната {unavailableRoom.Id} недоступна для бронирования.");

            var hasOverlap = await _context.Bookings
                .Include(b => b.BookingRooms)
                .AnyAsync(b =>
                    (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
                    b.StartDate < end &&
                    start < b.EndDate &&
                    b.BookingRooms.Any(br => roomIdArray.Contains(br.RoomId)));

            if (hasOverlap)
                return (false, "Одна или несколько комнат уже забронированы на эти даты.");

            // Создаём брони
            var booking = new Booking
            {
                UserId = userId,
                StartDate = start,
                EndDate = end
            };

            foreach (var room in rooms)
            {
                var bookingRoom = new BookingRoom
                {
                    RoomId = room.Id,
                    Booking = booking,
                    PriceAtBooking = room.PricePerDay
                };
                booking.BookingRooms.Add(bookingRoom);
            }

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
