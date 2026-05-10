using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TrainingCenter.Data;
using TrainingCenter.Models;

namespace TrainingCenter.Tests;

[Collection("Sequential")]
public class ReservationsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;

    public ReservationsControllerTests(WebApplicationFactory<Program> factory)
    {
        DataStore.Reset();
        _client = factory.CreateClient();
    }

    public void Dispose() => DataStore.Reset();

    // === GET /api/reservations ===

    [Fact]
    public async Task GetAll_ReturnsAllReservations()
    {
        var res = await _client.GetFromJsonAsync<List<Reservation>>("/api/reservations");
        Assert.NotNull(res);
        Assert.Equal(6, res.Count);
    }

    // === GET /api/reservations/{id} ===

    [Fact]
    public async Task GetById_Existing_ReturnsReservation()
    {
        var res = await _client.GetFromJsonAsync<Reservation>("/api/reservations/1");
        Assert.NotNull(res);
        Assert.Equal("Jan Nowak", res.OrganizerName);
    }

    [Fact]
    public async Task GetById_NonExisting_Returns404()
    {
        var r = await _client.GetAsync("/api/reservations/999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // === GET /api/reservations?filters ===

    [Fact]
    public async Task GetFiltered_ByDate()
    {
        var res = await _client.GetFromJsonAsync<List<Reservation>>("/api/reservations?date=2026-05-10");
        Assert.NotNull(res);
        Assert.Equal(3, res.Count); // rezerwacje 1, 2, 6
        Assert.All(res, r => Assert.Equal(new DateOnly(2026, 5, 10), r.Date));
    }

    [Fact]
    public async Task GetFiltered_ByStatus()
    {
        var res = await _client.GetFromJsonAsync<List<Reservation>>("/api/reservations?status=confirmed");
        Assert.NotNull(res);
        Assert.All(res, r => Assert.Equal("confirmed", r.Status, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetFiltered_ByRoomId()
    {
        var res = await _client.GetFromJsonAsync<List<Reservation>>("/api/reservations?roomId=1");
        Assert.NotNull(res);
        Assert.Equal(2, res.Count);
        Assert.All(res, r => Assert.Equal(1, r.RoomId));
    }

    [Fact]
    public async Task GetFiltered_AllFilters()
    {
        var res = await _client.GetFromJsonAsync<List<Reservation>>(
            "/api/reservations?date=2026-05-10&status=confirmed&roomId=1");
        Assert.NotNull(res);
        Assert.Single(res);
    }

    [Fact]
    public async Task GetFiltered_NoMatch_ReturnsEmpty()
    {
        var res = await _client.GetFromJsonAsync<List<Reservation>>("/api/reservations?date=2030-01-01");
        Assert.NotNull(res);
        Assert.Empty(res);
    }

    // === POST /api/reservations ===

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        var rsv = new Reservation
        {
            RoomId = 3, OrganizerName = "Test User", Topic = "Test Topic",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var created = await r.Content.ReadFromJsonAsync<Reservation>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.NotNull(r.Headers.Location);
    }

    [Fact]
    public async Task Create_NonExistingRoom_Returns404()
    {
        var rsv = new Reservation
        {
            RoomId = 999, OrganizerName = "Test", Topic = "Test",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Create_InactiveRoom_Returns400()
    {
        var rsv = new Reservation
        {
            RoomId = 4, OrganizerName = "Test", Topic = "Test",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // --- Kolizje czasowe ---

    [Fact]
    public async Task Create_ExactSameTime_Returns409()
    {
        // Sala 1 ma rezerwację 8:00-10:00 na 2026-05-10
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Kolizja",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Create_PartialOverlap_StartDuring_Returns409()
    {
        // Sala 1: 8:00-10:00 → nowa 9:00-11:00
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Partial",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Create_PartialOverlap_EndDuring_Returns409()
    {
        // Sala 1: 8:00-10:00 → nowa 7:00-9:00
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Partial",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(9, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Create_ContainingExisting_Returns409()
    {
        // Sala 1: 8:00-10:00 → nowa 7:00-11:00
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Containing",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(11, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Create_InsideExisting_Returns409()
    {
        // Sala 1: 8:00-10:00 → nowa 8:30-9:30
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Inside",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(9, 30),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Create_AdjacentAfter_NoConflict_Returns201()
    {
        // Sala 1: 8:00-10:00 → nowa 10:00-12:00 (styk, nie kolizja)
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Adjacent",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Create_AdjacentBefore_NoConflict_Returns201()
    {
        // Sala 1: 8:00-10:00 → nowa 6:00-8:00 (styk, nie kolizja)
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Adjacent Before",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(8, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Create_SameTimeDifferentDay_Returns201()
    {
        // Sala 1: 8:00-10:00 na 2026-05-10 → ten sam czas na 2026-05-20
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Other Day",
            Date = new DateOnly(2026, 5, 20),
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Create_SameTimeDifferentRoom_Returns201()
    {
        // Sala 1: 8:00-10:00 na 2026-05-10 → Sala 3 ten sam czas
        var rsv = new Reservation
        {
            RoomId = 3, OrganizerName = "Test", Topic = "Other Room",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Create_OverlapWithCancelled_NoConflict_Returns201()
    {
        // Rezerwacja 6 (sala 2, 14:00-16:00, cancelled) → nowa w tym samym czasie
        var rsv = new Reservation
        {
            RoomId = 2, OrganizerName = "Test", Topic = "Over cancelled",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(16, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    // --- Walidacja ---

    [Fact]
    public async Task Create_EmptyOrganizer_Returns400()
    {
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "", Topic = "Test",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyTopic_Returns400()
    {
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Create_EndBeforeStart_Returns400()
    {
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Bad time",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(10, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Create_EndEqualStart_Returns400()
    {
        var rsv = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Same time",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(10, 0),
            Status = "planned"
        };
        var r = await _client.PostAsJsonAsync("/api/reservations", rsv);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // === PUT /api/reservations/{id} ===

    [Fact]
    public async Task Update_Existing_Returns200()
    {
        var upd = new Reservation
        {
            RoomId = 1, OrganizerName = "Zmieniony", Topic = "Nowy temat",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0),
            Status = "confirmed"
        };
        var r = await _client.PutAsJsonAsync("/api/reservations/1", upd);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var result = await r.Content.ReadFromJsonAsync<Reservation>();
        Assert.NotNull(result);
        Assert.Equal("Zmieniony", result.OrganizerName);
    }

    [Fact]
    public async Task Update_NonExisting_Returns404()
    {
        var upd = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Test",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Status = "planned"
        };
        var r = await _client.PutAsJsonAsync("/api/reservations/999", upd);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Update_ToConflictingTime_Returns409()
    {
        // Rezerwacja 3 (sala 1, 2026-05-11 14:00-15:30) → zmień na czas rezerwacji 1
        var upd = new Reservation
        {
            RoomId = 1, OrganizerName = "Test", Topic = "Conflict",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0),
            Status = "planned"
        };
        var r = await _client.PutAsJsonAsync("/api/reservations/3", upd);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Update_SameTimeAsSelf_Returns200()
    {
        // Aktualizacja rezerwacji 1 bez zmiany czasu
        var upd = new Reservation
        {
            RoomId = 1, OrganizerName = "Updated Name", Topic = "Updated Topic",
            Date = new DateOnly(2026, 5, 10),
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0),
            Status = "confirmed"
        };
        var r = await _client.PutAsJsonAsync("/api/reservations/1", upd);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Update_ToNonExistingRoom_Returns404()
    {
        var upd = new Reservation
        {
            RoomId = 999, OrganizerName = "Test", Topic = "Test",
            Date = new DateOnly(2026, 6, 1),
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Status = "planned"
        };
        var r = await _client.PutAsJsonAsync("/api/reservations/1", upd);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // === DELETE /api/reservations/{id} ===

    [Fact]
    public async Task Delete_Existing_Returns204()
    {
        var r = await _client.DeleteAsync("/api/reservations/1");
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        var get = await _client.GetAsync("/api/reservations/1");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExisting_Returns404()
    {
        var r = await _client.DeleteAsync("/api/reservations/999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGetAll_ReturnsFewerReservations()
    {
        await _client.DeleteAsync("/api/reservations/1");
        var res = await _client.GetFromJsonAsync<List<Reservation>>("/api/reservations");
        Assert.NotNull(res);
        Assert.Equal(5, res.Count);
    }
}
