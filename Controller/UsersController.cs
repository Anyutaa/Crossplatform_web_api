using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Http;
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
        // 🔹 Эндпоинт для входа
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userService.AuthenticateUserAsync(request.Email, request.Password);
            if (user == null)
                return Unauthorized("Неверный email или пароль");

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Role
            });
        }

        public class LoginRequest
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;
        }
        // 🔹 Получить всех пользователей
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        // 🔹 Получить пользователя по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("Пользователь не найден.");
            return Ok(user);
        }

        // 🔹 Создать нового пользователя
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                Email = request.Email,
                Name = request.Name,
                Role = UserRole.User 
            };

            var (success, error) = await _userService.CreateUserAsync(user, request.Password);
            if (!success)
                return BadRequest(error);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Role
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
        {
            if (id != updatedUser.Id)
                return BadRequest("ID в пути не совпадает с ID в теле запроса.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, error) = await _userService.UpdateUserAsync(updatedUser);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь успешно обновлен");
        }
        // 🔹 Мягкое удаление пользователя
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, error) = await _userService.DeleteUserAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Пользователь успешно удалён (мягкое удаление).");
        }

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
