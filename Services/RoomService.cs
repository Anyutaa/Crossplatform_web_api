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

        public async Task<(bool success, string? error)> UpdateRoomAsync(int roomId, UpdateRoomRequest request, int currentUserId)
        {
            var existingRoom = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (existingRoom == null)
                return (false, "Комната не найдена.");

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return (false, "Пользователь не найден.");

            bool isAdmin = currentUser.Role == UserRole.Admin;
            bool isOwner = existingRoom.OwnerId == currentUser.Id;

            if (!isAdmin && !isOwner)
                return (false, "Недостаточно прав для редактирования этой комнаты.");

            if (currentUser.Status != UserStatus.Active)
                return (false, "Пользователь неактивен и не может редактировать комнаты.");
            
            if (existingRoom.Status == RoomStatus.Archived)
            {
                if (!request.Status.HasValue || request.Status.Value == RoomStatus.Archived)
                    return (false, "Архивированную комнату нельзя редактировать.");
                if (!isAdmin && !isOwner)
                    return (false, "Недостаточно прав для разархивации комнаты.");
                existingRoom.Status = request.Status.Value;
            }

            // Частичное обновление
            if (!string.IsNullOrWhiteSpace(request.Name))
                existingRoom.Name = request.Name.Trim();

            if (request.PricePerDay.HasValue)
                existingRoom.PricePerDay = request.PricePerDay.Value;

            // Обработка статуса с учетом прав
            if (request.Status.HasValue)
            {
                if (isAdmin)
                {
                    existingRoom.Status = request.Status.Value;
                }
                else if (isOwner)
                {
                    if (request.Status.Value == RoomStatus.Available ||
                        request.Status.Value == RoomStatus.Maintenance ||
                        request.Status.Value == RoomStatus.Archived)
                    {
                        existingRoom.Status = request.Status.Value;
                    }
                    else
                    {
                        return (false, "Владелец не может установить данный статус комнаты.");
                    }
                }
            }

            // Для админа - возможность сменить владельца
            if (isAdmin && request.OwnerId.HasValue)
            {
                var newOwner = await _context.Users.FindAsync(request.OwnerId.Value);
                if (newOwner != null)
                {
                    existingRoom.OwnerId = request.OwnerId.Value;
                }
                else
                {
                    return (false, "Новый владелец не найден.");
                }
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

        public async Task<List<Room>> GetRoomsForUserAsync(int currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
                return new List<Room>();

            bool isAdmin = currentUser.Role == UserRole.Admin;

            // Если админ — возвращаем все комнаты, иначе только свои
            return await _context.Rooms
                .Where(r => isAdmin || r.OwnerId == currentUserId)
                .OrderBy(r => r.Id)
                .ToListAsync();
        }


        public async Task<Room?> GetRoomByIdAsync(int roomId)
        {
            return await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == roomId && r.Status != RoomStatus.Archived);
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

        public class UpdateRoomRequest
        {
            public string? Name { get; set; }
            public decimal? PricePerDay { get; set; }
            public RoomStatus? Status { get; set; }
            public int? OwnerId { get; set; } 
        }
    }
}
