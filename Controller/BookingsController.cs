using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Http;
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


        [HttpPost]
        public async Task<IActionResult> CreateBooking(int userId, int roomId, DateTime start, DateTime end)
        {
            var (success, error) = await _bookingService.CreateBookingAsync(userId, roomId, start, end);
            if (!success) return BadRequest(error);
            return Ok("Бронирование успешно создано");
        }

        [HttpGet]
        public async Task<IActionResult> GetBookings()
        {
            var bookings = await _bookingService.GetAllBookingsAsync();
            return Ok(bookings);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            var booking = await _bookingService.GetBookingAsync(id);
            if (booking == null)
                return NotFound("Бронь не найдена.");
            return Ok(booking);
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var (success, error) = await _bookingService.CancelBookingAsync(id);
            if (!success) return BadRequest(error);
            return Ok("Бронь успешно отменена");
        }

        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var (success, error) = await _bookingService.ConfirmBookingAsync(id);
            if (!success) return BadRequest(error);
            return Ok("Бронь подтверждена");
        }
    }
}
