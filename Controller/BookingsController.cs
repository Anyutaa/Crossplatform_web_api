using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Mvc;

namespace Crossplatform_2_smirnova.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly BookingService _bookingService;

        public BookingsController(BookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // Получить все брони (для админа — все, для пользователя — только свои)
        [HttpGet]
        public async Task<IActionResult> GetBookings([FromQuery] int currentUserId)
        {
            var bookings = await _bookingService.GetAllBookingsAsync(currentUserId);
            return Ok(bookings);
        }

        // Получить бронь по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id, [FromQuery] int currentUserId)
        {
            var booking = await _bookingService.GetBookingAsync(id, currentUserId);
            if (booking == null)
                return NotFound("Бронь не найдена или недоступна для текущего пользователя.");
            return Ok(booking);
        }

        // Создание брони
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromQuery] int userId, [FromQuery] int roomId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var (success, error) = await _bookingService.CreateBookingAsync(userId, roomId, start, end);
            if (!success) return BadRequest(error);
            return Ok("Бронирование успешно создано.");
        }

        // Отмена брони
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id, [FromQuery] int currentUserId)
        {
            var (success, error) = await _bookingService.CancelBookingAsync(id, currentUserId);
            if (!success) return BadRequest(error);
            return Ok("Бронь успешно отменена.");
        }

        // Подтверждение брони (только админ)
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int id, [FromQuery] int currentUserId)
        {
            var (success, error) = await _bookingService.ConfirmBookingAsync(id, currentUserId);
            if (!success) return BadRequest(error);
            return Ok("Бронь подтверждена.");
        }
    }
}
