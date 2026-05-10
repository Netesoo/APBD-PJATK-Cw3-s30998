using Microsoft.AspNetCore.Mvc;
using TrainingCenter.Data;
using TrainingCenter.Models;

namespace TrainingCenter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    // GET /api/rooms
    // GET /api/rooms?minCapacity=20&hasProjector=true&activeOnly=true
    [HttpGet]
    public ActionResult<IEnumerable<Room>> GetAll(
        [FromQuery] int? minCapacity,
        [FromQuery] bool? hasProjector,
        [FromQuery] bool? activeOnly)
    {
        var rooms = DataStore.Rooms.AsEnumerable();

        if (minCapacity.HasValue)
            rooms = rooms.Where(r => r.Capacity >= minCapacity.Value);

        if (hasProjector.HasValue)
            rooms = rooms.Where(r => r.HasProjector == hasProjector.Value);

        if (activeOnly.HasValue && activeOnly.Value)
            rooms = rooms.Where(r => r.IsActive);

        return Ok(rooms.ToList());
    }

    // GET /api/rooms/{id}
    [HttpGet("{id:int}")]
    public ActionResult<Room> GetById(int id)
    {
        var room = DataStore.Rooms.FirstOrDefault(r => r.Id == id);
        if (room == null)
            return NotFound(new { message = $"Sala o id {id} nie została znaleziona." });

        return Ok(room);
    }

    // GET /api/rooms/building/{buildingCode}
    [HttpGet("building/{buildingCode}")]
    public ActionResult<IEnumerable<Room>> GetByBuilding(string buildingCode)
    {
        var rooms = DataStore.Rooms
            .Where(r => r.BuildingCode.Equals(buildingCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Ok(rooms);
    }

    // POST /api/rooms
    [HttpPost]
    public ActionResult<Room> Create([FromBody] Room room)
    {
        room.Id = DataStore.NextRoomId();
        DataStore.Rooms.Add(room);

        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    // PUT /api/rooms/{id}
    [HttpPut("{id:int}")]
    public ActionResult<Room> Update(int id, [FromBody] Room room)
    {
        var existing = DataStore.Rooms.FirstOrDefault(r => r.Id == id);
        if (existing == null)
            return NotFound(new { message = $"Sala o id {id} nie została znaleziona." });

        existing.Name = room.Name;
        existing.BuildingCode = room.BuildingCode;
        existing.Floor = room.Floor;
        existing.Capacity = room.Capacity;
        existing.HasProjector = room.HasProjector;
        existing.IsActive = room.IsActive;

        return Ok(existing);
    }

    // DELETE /api/rooms/{id}
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var existing = DataStore.Rooms.FirstOrDefault(r => r.Id == id);
        if (existing == null)
            return NotFound(new { message = $"Sala o id {id} nie została znaleziona." });

        // Sprawdź, czy istnieją powiązane rezerwacje
        var hasReservations = DataStore.Reservations.Any(r => r.RoomId == id);
        if (hasReservations)
            return Conflict(new { message = "Nie można usunąć sali, ponieważ posiada powiązane rezerwacje." });

        DataStore.Rooms.Remove(existing);
        return NoContent();
    }
}
