using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Crossplatform_2_smirnova.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class BookingsController : ControllerBase
    {
        private readonly BookingService _bookingService;

        public BookingsController(BookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetCurrentUserRole() =>
            User.FindFirstValue(ClaimTypes.Role)!;

        // Получить все брони (для админа — все, для пользователя — только свои)
        [HttpGet]
        public async Task<IActionResult> GetBookings()
        {
            var currentUserId = GetCurrentUserId();
            var bookings = await _bookingService.GetAllBookingsAsync(currentUserId);
            return Ok(bookings);
        }

        // Получить бронь по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            var currentUserId = GetCurrentUserId();
            var booking = await _bookingService.GetBookingAsync(id, currentUserId);
            if (booking == null)
                return NotFound("Бронь не найдена или недоступна для текущего пользователя.");
            return Ok(booking);
        }

        // Создание брони (только активные пользователи)
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> CreateBooking([FromQuery] int roomId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var userId = GetCurrentUserId();
            var (success, error) = await _bookingService.CreateBookingAsync(userId, roomId, start, end);
            if (!success) return BadRequest(error);
            return Ok("Бронирование успешно создано.");
        }

        // Отмена брони
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = GetCurrentUserId();
            var (success, error) = await _bookingService.CancelBookingAsync(id, userId);
            if (!success) return BadRequest(error);
            return Ok("Бронь успешно отменена.");
        }

        // Подтверждение брони (только админ)
        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var userId = GetCurrentUserId();
            var (success, error) = await _bookingService.ConfirmBookingAsync(id, userId);
            if (!success) return BadRequest(error);
            return Ok("Бронь подтверждена.");
        }
    }
}
