using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet]
        public async Task<IActionResult> GetRooms()
        {
            var currentUserId = GetCurrentUserId();
            var rooms = await _roomService.GetRoomsForUserAsync(currentUserId);
            return Ok(rooms);
        }

        public class CreateRoomDto
        {
            public string Name { get; set; }
            public decimal PricePerDay { get; set; }
            public int Status { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            var ownerId = GetCurrentUserId();
            var (success, room, error) = await _roomService.CreateRoomAsync(ownerId, dto.Name, dto.PricePerDay);

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
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoomById(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);

            if (room == null)
                return NotFound($"Комната с ID {id} не найдена");

            return Ok(room);
        }
    }
}
