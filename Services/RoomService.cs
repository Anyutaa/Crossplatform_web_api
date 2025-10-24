using Microsoft.EntityFrameworkCore;
using Crossplatform_2_smirnova.Data;
using Crossplatform_2_smirnova.Models;

namespace Crossplatform_2_smirnova.Services
{
    public class RoomService
    {
        private readonly ApplicationDbContext _context;

        public RoomService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, Room? room, string? error)> CreateRoomAsync(int ownerId, string name, decimal pricePerDay)
        {
            var owner = await _context.Users.FindAsync(ownerId);
            if (owner == null)
                return (false, null, "Владелец не найден.");

            if (owner.Status != UserStatus.Active)
                return (false, null, "Только активный пользователь может создавать комнаты.");

            if (string.IsNullOrWhiteSpace(name))
                return (false, null, "Название комнаты не может быть пустым.");

            if (pricePerDay < 0)
                return (false, null, "Цена не может быть отрицательной.");

            var room = new Room
            {
                OwnerId = ownerId,
                Name = name.Trim(),
                PricePerDay = pricePerDay,
                Status = RoomStatus.Available
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return (true, room, null);
        }

        public async Task<(bool success, string? error)> UpdateRoomAsync(Room updatedRoom, int currentUserId)
        {
            var existingRoom = await _context.Rooms.FindAsync(updatedRoom.Id);
            if (existingRoom == null)
                return (false, "Комната не найдена.");

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return (false, "Пользователь не найден.");

            if (existingRoom.Status == RoomStatus.Archived)
                return (false, "Нельзя редактировать архивированную комнату.");

            bool isAdmin = currentUser.Role == UserRole.Admin;
            bool isOwner = existingRoom.OwnerId == currentUser.Id;

            if (!isAdmin && !isOwner)
                return (false, "Недостаточно прав для редактирования этой комнаты.");

            if (currentUser.Status != UserStatus.Active)
                return (false, "Пользователь неактивен и не может редактировать комнаты.");


            existingRoom.Name = updatedRoom.Name.Trim();
            existingRoom.PricePerDay = updatedRoom.PricePerDay;

            if (isAdmin)
            {
                existingRoom.Status = updatedRoom.Status; 
                if (updatedRoom.OwnerId != existingRoom.OwnerId)
                    existingRoom.OwnerId = updatedRoom.OwnerId;
            }
            else if (isOwner)
            {
                if (updatedRoom.Status == RoomStatus.Maintenance || updatedRoom.Status == RoomStatus.Available)
                    existingRoom.Status = updatedRoom.Status;
                else
                    return (false, "Владелец не может архивировать комнату.");
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string? error)> ArchiveRoomAsync(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return (false, "Комната не найдена.");

            room.Archive();

            // Отменяем все активные брони для этой комнаты
            var activeBookings = await _context.Bookings
                .Where(b => b.BookingRooms.Any(br => br.RoomId == id)
                            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                .ToListAsync();

            foreach (var booking in activeBookings)
                booking.Cancel();

            await _context.SaveChangesAsync();
            return (true, null);
        }

        // Получить список всех доступных комнат
        public async Task<List<Room>> GetAllAvailableRoomsAsync()
        {
            return await _context.Rooms
                .Where(r => r.Status == RoomStatus.Available)
                .ToListAsync();
        }
        public async Task<Room?> GetRoomByIdAsync(int id, int currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return null;

            bool isAdmin = currentUser.Role == UserRole.Admin;

            if (isAdmin)
                return await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);

            return await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id && r.Status != RoomStatus.Archived);
        }

        // Проверка доступности комнаты на даты
        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime start, DateTime end)
        {
            if (start >= end)
                throw new ArgumentException("Дата начала должна быть раньше даты окончания.");

            bool overlapping = await _context.Bookings
                .Where(b => b.BookingRooms.Any(br => br.RoomId == roomId)
                            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                            && b.StartDate < end && start < b.EndDate)
                .AnyAsync();

            return !overlapping;
        }
    }
}
