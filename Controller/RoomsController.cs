using Crossplatform_2_smirnova.Models;
using Crossplatform_2_smirnova.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoom(int id)
        {
            var room = await _roomService.GetRoomAsync(id);
            if (room == null)
                return NotFound("Комната не найдена.");
            return Ok(room);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom(int ownerId, string name, decimal pricePerDay)
        {
            var (success, error) = await _roomService.CreateRoomAsync(ownerId, name, pricePerDay);
            if (!success) return BadRequest(error);
            return Ok("Комната успешно создана");
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] Room updatedRoom)
        {
            if (id != updatedRoom.Id)
                return BadRequest("ID в пути не совпадает с ID в теле запроса.");

            var (success, error) = await _roomService.UpdateRoomAsync(updatedRoom);
            if (!success)
                return BadRequest(error);

            return Ok("Комната успешно обновлена");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var (success, error) = await _roomService.DeleteRoomAsync(id);
            if (!success)
                return BadRequest(error);

            return Ok("Комната успешно удалена");
        }
    }
}
