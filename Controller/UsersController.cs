using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Crossplatform_2_smirnova.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> Register([FromBody] CreateUserRequest request)
        {
            var (success, user, error) = await _userService.CreateUserAsync(
                request.Email, request.Name, request.Password);

            if (!success)
                return BadRequest(new { error });

            return Ok(new { message = "User registered successfully", user = new { user.Id, user.Email, user.Name } });
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetCurrentUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

        // Получить всех пользователей (только для админа)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] bool includeArchived = false)
        {
            var users = await _userService.GetAllUsersAsync();
            if (!includeArchived)
                users = users.Where(u => u.Status != UserStatus.Archived).ToList();

            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("Пользователь не найден.");

            return Ok(user);
        }



        [HttpPut("{id}/update")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequest request)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId != id)
                return StatusCode(403, "Нет доступа для обновления данных другого пользователя.");

            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser == null)
                return NotFound("Пользователь не найден.");

            var currentUser = await _userService.GetUserByIdAsync(currentUserId);

            existingUser.Email = request.Email;
            existingUser.Name = request.Name;

            var (success, error) = await _userService.UpdateUserAsync(existingUser, currentUser);
            if (!success)
                return BadRequest(new { error });

            return Ok(new
            {
                message = "Пользователь успешно обновлён",
                user = new { existingUser.Id, existingUser.Email, existingUser.Name }
            });
        }

        [HttpPut("{id}/change-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleUserRole(int id)
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("Пользователь не найден.");

            var oldRole = user.Role;
            user.Role = user.Role == UserRole.Admin ? UserRole.User : UserRole.Admin;
            var (success, error) = await _userService.UpdateUserAsync(user, currentUser);
            if (!success)
                return BadRequest(new { error });

            return Ok(new
            {
                message = $"Роль пользователя успешно изменена с '{oldRole}' на '{user.Role}'",
                user = new { user.Id, user.Email, user.Name, user.Role }
            });
        }

        [HttpPut("{id}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveUser(int id)
        {
            var (success, error) = await _userService.ArchiveUserAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь и связанные данные успешно архивированы.");
        }

        [HttpPut("{id}/block")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var (success, error) = await _userService.BlockUserAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь и его комнаты заблокированы, активные брони отменены.");
        }

        [HttpPut("{id}/unblock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var (success, error) = await _userService.UnblockUserAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь и его комнаты разблокированы.");
        }


        public class CreateUserRequest { 
            [Required, EmailAddress] 
            public string Email { get; set; } = string.Empty; 
            [Required, StringLength(50)] 
            public string Name { get; set; } = string.Empty; 
            [Required, MinLength(8)] 
            public string Password { get; set; } = string.Empty; 
        }

        public class UserUpdateRequest
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, StringLength(50)]
            public string Name { get; set; } = string.Empty;

        }

    }
}

        
