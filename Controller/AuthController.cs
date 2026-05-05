using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static Crossplatform_2_smirnova.Controllers.UsersController;

namespace Crossplatform_2_smirnova.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly AuthOptions _authOptions;

        public AuthController(UserService userService, IOptions<AuthOptions> authOptions)
        {
            _userService = userService;
            _authOptions = authOptions.Value;
        }
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> Register([FromBody] CreateUserRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is null");
            }
            var (success, user, error) = await _userService.CreateUserAsync(
                request.Email, request.Name, request.Password, request.TelegramId, request.TelegramUsername);

            if (!success)
                return BadRequest(new { error });
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                user = new { user.Id, user.Email, user.Name, user.TelegramId, user.TelegramUsername }
            });
        }
        [HttpGet("telegram/{tgId}")]
        [AllowAnonymous]
        public IActionResult CheckTelegram(long tgId)
        {
            var user = _userService.GetByTelegramId(tgId);
            if (user == null)
                return NotFound(new { message = "Пользователь с этим TelegramId не найден" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.Name,
                    user.TelegramUsername
                }
            });
        }
        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userService.AuthenticateUserAsync(request.Email, request.Password);
            if (user == null)
                return Unauthorized("Неверный email или пароль");

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // Создаём токен
            var jwt = new JwtSecurityToken(
                issuer: _authOptions.Issuer,
                audience: _authOptions.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(_authOptions.LifetimeInHours),
                signingCredentials: new SigningCredentials(_authOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        public class CreateUserRequest
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
            [Required, StringLength(50)]
            public string Name { get; set; } = string.Empty;
            [Required, MinLength(6)]
            public string Password { get; set; } = string.Empty;
            public long? TelegramId { get; set; }
            public string? TelegramUsername { get; set; }
        }

    }
}


