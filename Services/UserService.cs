using Microsoft.EntityFrameworkCore;
using Crossplatform_2_smirnova.Data;
using Crossplatform_2_smirnova.Models;
using BCrypt.Net;

namespace Crossplatform_2_smirnova.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Получить всех активных пользователей
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.Status == UserStatus.Active)
                .ToListAsync();
        }

        // Получить одного пользователя по ID (только если не архивирован)
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Status != UserStatus.Archived);
        }

        // Добавить нового пользователя
        public async Task<(bool success, User? user, string? error)> CreateUserAsync(string email, string name, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return (false, null, "Пользователь с таким email уже существует.");

            var user = new User
            {
                Email = email,
                Name = name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, user, null);
        }


        // Обновить пользователя
        public async Task<(bool success, string? error)> UpdateUserAsync(User updatedUser, User currentUser)
        {
            var existingUser = await _context.Users.FindAsync(updatedUser.Id);
            if (existingUser == null)
                return (false, "Пользователь не найден.");

            // Нельзя редактировать архивированного пользователя
            if (existingUser.Status == UserStatus.Archived)
                return (false, "Нельзя редактировать архивированного пользователя.");

            // Проверка email на уникальность
            if (existingUser.Email != updatedUser.Email &&
                await _context.Users.AnyAsync(u => u.Email == updatedUser.Email))
                return (false, "Пользователь с таким email уже существует.");

            // Обновляем поля
            existingUser.Email = updatedUser.Email;
            existingUser.Name = updatedUser.Name;

            // Только админ может менять роль и статус
            if (currentUser.Role == UserRole.Admin)
            {
                existingUser.Role = updatedUser.Role;
                existingUser.Status = updatedUser.Status;
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }


        // Архивирование пользователя
        public async Task<(bool success, string? error)> ArchiveUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "Пользователь не найден.");

            if (user.Status == UserStatus.Archived)
                return (false, "Пользователь уже архивирован.");

            user.Archive();

            var rooms = await _context.Rooms
                .Where(r => r.OwnerId == userId && r.Status != RoomStatus.Archived)
                .ToListAsync();

            foreach (var room in rooms)
                room.Archive();

            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                .ToListAsync();

            foreach (var booking in bookings)
                booking.Cancel();

            await _context.SaveChangesAsync();
            return (true, null);
        }


        public async Task<(bool success, string? error)> BlockUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return (false, "Пользователь не найден.");

            user.Block();

            var rooms = await _context.Rooms
                .Where(r => r.OwnerId == id && r.Status == RoomStatus.Available)
                .ToListAsync();
            foreach (var room in rooms)
                room.SetBlocked();

            var bookings = await _context.Bookings
                .Where(b => b.UserId == id && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                .ToListAsync();
            foreach (var booking in bookings)
                booking.Cancel();

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string? error)> UnblockUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return (false, "Пользователь не найден.");

            user.Unblock();

            var rooms = await _context.Rooms
                .Where(r => r.OwnerId == id && r.Status == RoomStatus.Blocked)
                .ToListAsync();
            foreach (var room in rooms)
                room.Restore();

            await _context.SaveChangesAsync();
            return (true, null);
        }

    }
}
