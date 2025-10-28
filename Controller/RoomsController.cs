using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Crossplatform_2_smirnova.Services.RoomService;

namespace Crossplatform_2_smirnova.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class RoomsController : ControllerBase
    {
        private readonly RoomService _roomService;

        public RoomsController(RoomService roomService)
        {
            _roomService = roomService;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoom(int id)
        {
            var currentUserId = GetCurrentUserId();
            var room = await _roomService.GetRoomByIdAsync(id, currentUserId);

            if (room == null)
                return NotFound("Комната не найдена или недоступна для текущего пользователя.");

            return Ok(room);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromQuery] string name, [FromQuery] decimal pricePerDay)
        {
            var ownerId = GetCurrentUserId();
            var (success, room, error) = await _roomService.CreateRoomAsync(ownerId, name, pricePerDay);

            if (!success)
                return BadRequest(error);

            return Ok(room);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
        {
            if (request == null)
                return BadRequest("Тело запроса не может быть пустым.");

            var currentUserId = GetCurrentUserId();
            var (success, error) = await _roomService.UpdateRoomAsync(id, request, currentUserId);

            if (!success)
                return BadRequest(error);

            return Ok("Комната успешно обновлена.");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveRoom(int id)
        {
            var (success, error) = await _roomService.ArchiveRoomAsync(id);

            if (!success)
                return BadRequest(error);

            return Ok("Комната успешно архивирована и активные брони отменены.");
        }

        [AllowAnonymous] 
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableRooms()
        {
            var rooms = await _roomService.GetAllAvailableRoomsAsync();
            return Ok(rooms);
        }
    }
}
