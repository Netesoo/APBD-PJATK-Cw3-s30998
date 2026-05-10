using Microsoft.AspNetCore.Mvc;
using TrainingCenter.Data;
using TrainingCenter.Models;

namespace TrainingCenter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    // GET /api/reservations
    // GET /api/reservations?date=2026-05-10&status=confirmed&roomId=2
    [HttpGet]
    public ActionResult<IEnumerable<Reservation>> GetAll(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        [FromQuery] int? roomId)
    {
        var reservations = DataStore.Reservations.AsEnumerable();

        if (date.HasValue)
            reservations = reservations.Where(r => r.Date == date.Value);

        if (!string.IsNullOrWhiteSpace(status))
            reservations = reservations.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        if (roomId.HasValue)
            reservations = reservations.Where(r => r.RoomId == roomId.Value);

        return Ok(reservations.ToList());
    }

    // GET /api/reservations/{id}
    [HttpGet("{id:int}")]
    public ActionResult<Reservation> GetById(int id)
    {
        var reservation = DataStore.Reservations.FirstOrDefault(r => r.Id == id);
        if (reservation == null)
            return NotFound(new { message = $"Rezerwacja o id {id} nie została znaleziona." });

        return Ok(reservation);
    }

    // POST /api/reservations
    [HttpPost]
    public ActionResult<Reservation> Create([FromBody] Reservation reservation)
    {
        // Sprawdź, czy sala istnieje
        var room = DataStore.Rooms.FirstOrDefault(r => r.Id == reservation.RoomId);
        if (room == null)
            return NotFound(new { message = $"Sala o id {reservation.RoomId} nie istnieje." });

        // Sprawdź, czy sala jest aktywna
        if (!room.IsActive)
            return BadRequest(new { message = "Nie można zarezerwować nieaktywnej sali." });

        // Sprawdź kolizję czasową
        var conflict = DataStore.Reservations.Any(r =>
            r.RoomId == reservation.RoomId &&
            r.Date == reservation.Date &&
            r.Status != "cancelled" &&
            r.StartTime < reservation.EndTime &&
            r.EndTime > reservation.StartTime);

        if (conflict)
            return Conflict(new { message = "Rezerwacja koliduje czasowo z istniejącą rezerwacją tej sali." });

        reservation.Id = DataStore.NextReservationId();
        DataStore.Reservations.Add(reservation);

        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, reservation);
    }

    // PUT /api/reservations/{id}
    [HttpPut("{id:int}")]
    public ActionResult<Reservation> Update(int id, [FromBody] Reservation reservation)
    {
        var existing = DataStore.Reservations.FirstOrDefault(r => r.Id == id);
        if (existing == null)
            return NotFound(new { message = $"Rezerwacja o id {id} nie została znaleziona." });

        // Sprawdź, czy sala istnieje
        var room = DataStore.Rooms.FirstOrDefault(r => r.Id == reservation.RoomId);
        if (room == null)
            return NotFound(new { message = $"Sala o id {reservation.RoomId} nie istnieje." });

        // Sprawdź kolizję czasową (pomijając bieżącą rezerwację)
        var conflict = DataStore.Reservations.Any(r =>
            r.Id != id &&
            r.RoomId == reservation.RoomId &&
            r.Date == reservation.Date &&
            r.Status != "cancelled" &&
            r.StartTime < reservation.EndTime &&
            r.EndTime > reservation.StartTime);

        if (conflict)
            return Conflict(new { message = "Rezerwacja koliduje czasowo z istniejącą rezerwacją tej sali." });

        existing.RoomId = reservation.RoomId;
        existing.OrganizerName = reservation.OrganizerName;
        existing.Topic = reservation.Topic;
        existing.Date = reservation.Date;
        existing.StartTime = reservation.StartTime;
        existing.EndTime = reservation.EndTime;
        existing.Status = reservation.Status;

        return Ok(existing);
    }

    // DELETE /api/reservations/{id}
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var existing = DataStore.Reservations.FirstOrDefault(r => r.Id == id);
        if (existing == null)
            return NotFound(new { message = $"Rezerwacja o id {id} nie została znaleziona." });

        DataStore.Reservations.Remove(existing);
        return NoContent();
    }
}
