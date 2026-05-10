using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TrainingCenter.Data;
using TrainingCenter.Models;

namespace TrainingCenter.Tests;

[Collection("Sequential")]
public class RoomsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;

    public RoomsControllerTests(WebApplicationFactory<Program> factory)
    {
        DataStore.Reset();
        _client = factory.CreateClient();
    }

    public void Dispose() => DataStore.Reset();

    // === GET /api/rooms ===

    [Fact]
    public async Task GetAll_ReturnsAllRooms()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms");
        Assert.NotNull(rooms);
        Assert.Equal(5, rooms.Count);
    }

    // === GET /api/rooms/{id} ===

    [Fact]
    public async Task GetById_Existing_ReturnsRoom()
    {
        var room = await _client.GetFromJsonAsync<Room>("/api/rooms/1");
        Assert.NotNull(room);
        Assert.Equal("Aula 101", room.Name);
    }

    [Fact]
    public async Task GetById_NonExisting_Returns404()
    {
        var r = await _client.GetAsync("/api/rooms/999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task GetById_Negative_Returns404()
    {
        var r = await _client.GetAsync("/api/rooms/-1");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // === GET /api/rooms/building/{code} ===

    [Fact]
    public async Task GetByBuilding_Existing_ReturnsMatching()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms/building/A");
        Assert.NotNull(rooms);
        Assert.Equal(2, rooms.Count);
        Assert.All(rooms, r => Assert.Equal("A", r.BuildingCode));
    }

    [Fact]
    public async Task GetByBuilding_CaseInsensitive()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms/building/a");
        Assert.NotNull(rooms);
        Assert.Equal(2, rooms.Count);
    }

    [Fact]
    public async Task GetByBuilding_NonExisting_ReturnsEmpty()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms/building/Z");
        Assert.NotNull(rooms);
        Assert.Empty(rooms);
    }

    // === GET /api/rooms?filters ===

    [Fact]
    public async Task GetFiltered_MinCapacity()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms?minCapacity=30");
        Assert.NotNull(rooms);
        Assert.All(rooms, r => Assert.True(r.Capacity >= 30));
    }

    [Fact]
    public async Task GetFiltered_HasProjectorTrue()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms?hasProjector=true");
        Assert.NotNull(rooms);
        Assert.All(rooms, r => Assert.True(r.HasProjector));
    }

    [Fact]
    public async Task GetFiltered_HasProjectorFalse()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms?hasProjector=false");
        Assert.NotNull(rooms);
        Assert.Single(rooms);
        Assert.All(rooms, r => Assert.False(r.HasProjector));
    }

    [Fact]
    public async Task GetFiltered_ActiveOnly()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms?activeOnly=true");
        Assert.NotNull(rooms);
        Assert.Equal(4, rooms.Count);
        Assert.All(rooms, r => Assert.True(r.IsActive));
    }

    [Fact]
    public async Task GetFiltered_AllFilters()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>(
            "/api/rooms?minCapacity=20&hasProjector=true&activeOnly=true");
        Assert.NotNull(rooms);
        Assert.Equal(3, rooms.Count);
    }

    [Fact]
    public async Task GetFiltered_HighCapacity_ReturnsEmpty()
    {
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms?minCapacity=1000");
        Assert.NotNull(rooms);
        Assert.Empty(rooms);
    }

    // === POST /api/rooms ===

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        var room = new Room { Name = "Lab 204", BuildingCode = "B", Floor = 2, Capacity = 24, HasProjector = true, IsActive = true };
        var r = await _client.PostAsJsonAsync("/api/rooms", room);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var created = await r.Content.ReadFromJsonAsync<Room>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Lab 204", created.Name);
        Assert.NotNull(r.Headers.Location);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400()
    {
        var room = new Room { Name = "", BuildingCode = "B", Floor = 2, Capacity = 24, HasProjector = true, IsActive = true };
        var r = await _client.PostAsJsonAsync("/api/rooms", room);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyBuildingCode_Returns400()
    {
        var room = new Room { Name = "Test", BuildingCode = "", Floor = 2, Capacity = 24, HasProjector = true, IsActive = true };
        var r = await _client.PostAsJsonAsync("/api/rooms", room);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Create_ZeroCapacity_Returns400()
    {
        var room = new Room { Name = "Test", BuildingCode = "A", Floor = 1, Capacity = 0, HasProjector = false, IsActive = true };
        var r = await _client.PostAsJsonAsync("/api/rooms", room);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Create_NegativeCapacity_Returns400()
    {
        var room = new Room { Name = "Test", BuildingCode = "A", Floor = 1, Capacity = -5, HasProjector = false, IsActive = true };
        var r = await _client.PostAsJsonAsync("/api/rooms", room);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsCreated()
    {
        var room = new Room { Name = "Nowa", BuildingCode = "D", Floor = 1, Capacity = 50, HasProjector = true, IsActive = true };
        var cr = await _client.PostAsJsonAsync("/api/rooms", room);
        var created = await cr.Content.ReadFromJsonAsync<Room>();
        var fetched = await _client.GetFromJsonAsync<Room>($"/api/rooms/{created!.Id}");
        Assert.NotNull(fetched);
        Assert.Equal("Nowa", fetched.Name);
    }

    // === PUT /api/rooms/{id} ===

    [Fact]
    public async Task Update_Existing_Returns200()
    {
        var upd = new Room { Name = "Aula Główna", BuildingCode = "A", Floor = 1, Capacity = 200, HasProjector = true, IsActive = true };
        var r = await _client.PutAsJsonAsync("/api/rooms/1", upd);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var result = await r.Content.ReadFromJsonAsync<Room>();
        Assert.NotNull(result);
        Assert.Equal("Aula Główna", result.Name);
        Assert.Equal(200, result.Capacity);
    }

    [Fact]
    public async Task Update_NonExisting_Returns404()
    {
        var upd = new Room { Name = "X", BuildingCode = "X", Floor = 9, Capacity = 10, HasProjector = false, IsActive = true };
        var r = await _client.PutAsJsonAsync("/api/rooms/999", upd);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidData_Returns400()
    {
        var upd = new Room { Name = "", BuildingCode = "A", Floor = 1, Capacity = 0, HasProjector = true, IsActive = true };
        var r = await _client.PutAsJsonAsync("/api/rooms/1", upd);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // === DELETE /api/rooms/{id} ===

    [Fact]
    public async Task Delete_WithReservations_Returns409()
    {
        var r = await _client.DeleteAsync("/api/rooms/1");
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutReservations_Returns204()
    {
        var r = await _client.DeleteAsync("/api/rooms/4");
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        var get = await _client.GetAsync("/api/rooms/4");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExisting_Returns404()
    {
        var r = await _client.DeleteAsync("/api/rooms/999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGetAll_ReturnsFewerRooms()
    {
        await _client.DeleteAsync("/api/rooms/4");
        var rooms = await _client.GetFromJsonAsync<List<Room>>("/api/rooms");
        Assert.NotNull(rooms);
        Assert.Equal(4, rooms.Count);
        Assert.DoesNotContain(rooms, r => r.Id == 4);
    }
}
