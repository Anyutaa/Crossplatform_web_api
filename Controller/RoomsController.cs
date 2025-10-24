using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Mvc;

namespace Crossplatform_2_smirnova.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly RoomService _roomService;

        public RoomsController(RoomService roomService)
        {
            _roomService = roomService;
        }

        // Получить комнату по ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoom(int id, [FromQuery] int currentUserId)
        {
            var room = await _roomService.GetRoomByIdAsync(id, currentUserId);
            if (room == null)
                return NotFound("Комната не найдена или недоступна для текущего пользователя.");

            return Ok(room);
        }

        // Создать комнату
        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromQuery] int ownerId, [FromQuery] string name, [FromQuery] decimal pricePerDay)
        {
            var (success, room, error) = await _roomService.CreateRoomAsync(ownerId, name, pricePerDay);
            if (!success)
                return BadRequest(error);

            return Ok(room);
        }

        // Обновить комнату
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] Room updatedRoom, [FromQuery] int currentUserId)
        {
            if (id != updatedRoom.Id)
                return BadRequest("ID в пути не совпадает с ID в теле запроса.");

            var (success, error) = await _roomService.UpdateRoomAsync(updatedRoom, currentUserId);
            if (!success)
                return BadRequest(error);

            return Ok("Комната успешно обновлена.");
        }

        // Архивировать комнату
        [HttpDelete("{id}")]
        public async Task<IActionResult> ArchiveRoom(int id)
        {
            var (success, error) = await _roomService.ArchiveRoomAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Комната успешно архивирована и активные брони отменены.");
        }

        // Получить все доступные комнаты (для обычных пользователей)
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableRooms()
        {
            var rooms = await _roomService.GetAllAvailableRoomsAsync();
            return Ok(rooms);
        }
    }
}
