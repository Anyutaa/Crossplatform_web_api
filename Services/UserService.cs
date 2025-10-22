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

        // Получить всех пользователей
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        // Получить одного пользователя по ID
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        // Добавить нового пользователя
        public async Task<(bool success, string? error)> CreateUserAsync(User user, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return (false, "Пользователь с таким email уже существует.");

            // Хэширование пароля
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return (true, null);
        }
        // Метод аутентификации
        public async Task<User?> AuthenticateUserAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
                return null;

            // Проверка пароля
            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return user;

            return null;
        }
        // UserService.cs
        public async Task<(bool success, string? error)> UpdateUserAsync(User updatedUser)
        {
            var existingUser = await _context.Users.FindAsync(updatedUser.Id);
            if (existingUser == null)
                return (false, "Пользователь не найден.");

            // Проверяем email на уникальность (если изменился)
            if (existingUser.Email != updatedUser.Email &&
                await _context.Users.AnyAsync(u => u.Email == updatedUser.Email))
                return (false, "Пользователь с таким email уже существует.");

            // Обновляем разрешенные поля
            existingUser.Email = updatedUser.Email;
            existingUser.Name = updatedUser.Name;
            existingUser.Role = updatedUser.Role;
            existingUser.IsActive = updatedUser.IsActive;

            await _context.SaveChangesAsync();
            return (true, null);
        }
        // Удалить (мягко) пользователя
        public async Task<(bool success, string? error)> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return (false, "Пользователь не найден.");

            // Проверка: есть ли комнаты или брони
            bool hasRooms = await _context.Rooms.AnyAsync(r => r.OwnerId == id);
            bool hasBookings = await _context.Bookings.AnyAsync(b => b.UserId == id);

            if (hasRooms || hasBookings)
                return (false, "Нельзя удалить пользователя, у которого есть комнаты или брони.");

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}
