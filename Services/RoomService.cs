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
        //public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime start, DateTime end)
        //{
        //    if (start >= end)
        //        throw new ArgumentException("Дата начала должна быть раньше даты окончания.");

        //    //bool overlapping = await _context.Bookings
        //    //    .Where(b => b.RoomId == roomId &&
        //    //               (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
        //    //               b.StartDate < end &&
        //    //               start < b.EndDate)
        //    //    .AnyAsync();

        //    return !overlapping;
        //}

        public async Task<List<Room>> GetAllRoomsAsync()
        {
            return await _context.Rooms
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<Room?> GetRoomAsync(int id)
        {
            return await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        }

        public async Task<(bool success, string? error)> CreateRoomAsync(int ownerId, string name, decimal pricePerDay)
        {
            // Проверка бизнес-правил
            var owner = await _context.Users.FindAsync(ownerId);
            if (owner == null)
                return (false, "Владелец не найден.");

            if (!owner.IsActive) 
                return (false, "Владелец заблокирован и не может создавать комнаты.");


            // Создание объекта
            var room = new Room
            {
                OwnerId = ownerId,
                Name = name.Trim(),
                PricePerDay = pricePerDay,
                IsActive = true
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool success, string? error)> DeleteRoomAsync(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return (false, "Комната не найдена.");

            room.MarkAsDeleted();
            await _context.SaveChangesAsync();
            return (true, null);
        }

        //public async Task<bool> CheckAvailabilityAsync(int roomId, DateTime start, DateTime end)
        //{
        //    // Проверяем существование комнаты
        //    var room = await _context.Rooms.FindAsync(roomId);
        //    if (room == null || !room.IsActive)
        //        return false;

        //    // Проверяем корректность дат
        //    if (start >= end)
        //        return false;

        //    //// Проверяем нет ли пересекающихся бронирований
        //    //bool hasOverlappingBookings = await _context.Bookings
        //    //    .Where(b => b.RoomId == roomId &&
        //    //               (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
        //    //               b.StartDate < end &&
        //    //               start < b.EndDate)
        //    //    .AnyAsync();

        //    return !hasOverlappingBookings;
        //}

        public async Task<(bool success, string? error)> UpdateRoomAsync(Room updatedRoom)
        {
            var existingRoom = await _context.Rooms.FindAsync(updatedRoom.Id);
            if (existingRoom == null)
                return (false, "Комната не найдена.");

            // Проверяем, что владелец существует
            var owner = await _context.Users.FindAsync(updatedRoom.OwnerId);
            if (owner == null)
                return (false, "Владелец не найден.");

            // Обновляем поля
            existingRoom.Name = updatedRoom.Name;
            existingRoom.PricePerDay = updatedRoom.PricePerDay;
            existingRoom.OwnerId = updatedRoom.OwnerId;

            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
