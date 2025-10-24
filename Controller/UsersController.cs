using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Crossplatform_2_smirnova.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // Получить всех пользователей (для администратора)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeArchived = false)
        {
            var users = await _userService.GetAllUsersAsync();
            if (!includeArchived)
                users = users.Where(u => u.Status != UserStatus.Archived).ToList();

            return Ok(users);
        }

        // Получить пользователя по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] bool includeArchived = false)
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null || (!includeArchived && user.Status == UserStatus.Archived))
                return NotFound("Пользователь не найден.");

            return Ok(user);
        }

        // Создать нового пользователя
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, user, error) = await _userService.CreateUserAsync(
                request.Email, request.Name, request.Password);

            if (!success)
                return BadRequest(error);

            return CreatedAtAction(nameof(GetById), new { id = user!.Id }, new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Role,
                user.Status
            });
        }

        // Обновить пользователя
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] User updatedUser, [FromQuery] int currentUserId)
        {
            if (id != updatedUser.Id)
                return BadRequest("ID в пути не совпадает с ID пользователя.");

            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null)
                return BadRequest("Текущий пользователь не найден.");

            var (success, error) = await _userService.UpdateUserAsync(updatedUser, currentUser);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь успешно обновлён.");
        }

        // Архивация пользователя (только админ)
        [HttpPut("{id}/archive")]
        public async Task<IActionResult> ArchiveUser(int id, [FromQuery] int currentUserId)
        {
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || currentUser.Role != UserRole.Admin)
                return Forbid("Только администратор может архивировать пользователей.");

            var (success, error) = await _userService.ArchiveUserAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь и связанные данные успешно архивированы.");
        }

        // Заблокировать пользователя (только админ)
        [HttpPut("{id}/block")]
        public async Task<IActionResult> BlockUser(int id, [FromQuery] int currentUserId)
        {
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || currentUser.Role != UserRole.Admin)
                return Forbid("Только администратор может блокировать пользователей.");

            var (success, error) = await _userService.BlockUserAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь и его комнаты заблокированы, активные брони отменены.");
        }

        // Разблокировать пользователя (только админ)
        [HttpPut("{id}/unblock")]
        public async Task<IActionResult> UnblockUser(int id, [FromQuery] int currentUserId)
        {
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            if (currentUser == null || currentUser.Role != UserRole.Admin)
                return Forbid("Только администратор может разблокировать пользователей.");

            var (success, error) = await _userService.UnblockUserAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь и его комнаты разблокированы.");
        }

        // DTO для создания пользователя
        public class CreateUserRequest
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, StringLength(50)]
            public string Name { get; set; } = string.Empty;

            [Required, MinLength(8)]
            public string Password { get; set; } = string.Empty;
        }
    }
}
