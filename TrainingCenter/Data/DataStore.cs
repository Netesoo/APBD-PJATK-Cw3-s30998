using TrainingCenter.Models;

namespace TrainingCenter.Data;

public static class DataStore
{
    public static List<Room> Rooms { get; private set; } = CreateInitialRooms();
    public static List<Reservation> Reservations { get; private set; } = CreateInitialReservations();

    private static int _nextRoomId = 6;
    private static int _nextReservationId = 7;

    public static int NextRoomId() => _nextRoomId++;
    public static int NextReservationId() => _nextReservationId++;

    /// <summary>
    /// Resetuje dane do stanu początkowego. Używane w testach.
    /// </summary>
    public static void Reset()
    {
        Rooms = CreateInitialRooms();
        Reservations = CreateInitialReservations();
        _nextRoomId = 6;
        _nextReservationId = 7;
    }

    private static List<Room> CreateInitialRooms() => new()
    {
        new Room { Id = 1, Name = "Aula 101", BuildingCode = "A", Floor = 1, Capacity = 120, HasProjector = true, IsActive = true },
        new Room { Id = 2, Name = "Lab 202", BuildingCode = "B", Floor = 2, Capacity = 30, HasProjector = true, IsActive = true },
        new Room { Id = 3, Name = "Sala 303", BuildingCode = "A", Floor = 3, Capacity = 15, HasProjector = false, IsActive = true },
        new Room { Id = 4, Name = "Sala 104", BuildingCode = "C", Floor = 1, Capacity = 40, HasProjector = true, IsActive = false },
        new Room { Id = 5, Name = "Lab 305", BuildingCode = "B", Floor = 3, Capacity = 25, HasProjector = true, IsActive = true }
    };

    private static List<Reservation> CreateInitialReservations() => new()
    {
        new Reservation { Id = 1, RoomId = 1, OrganizerName = "Jan Nowak", Topic = "Wykład z algorytmów", Date = new DateOnly(2026, 5, 10), StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0), Status = "confirmed" },
        new Reservation { Id = 2, RoomId = 2, OrganizerName = "Anna Kowalska", Topic = "Warsztaty z REST API", Date = new DateOnly(2026, 5, 10), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 30), Status = "planned" },
        new Reservation { Id = 3, RoomId = 1, OrganizerName = "Piotr Wiśniewski", Topic = "Konsultacje projektowe", Date = new DateOnly(2026, 5, 11), StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 30), Status = "confirmed" },
        new Reservation { Id = 4, RoomId = 3, OrganizerName = "Maria Zielińska", Topic = "Spotkanie zespołu", Date = new DateOnly(2026, 5, 12), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), Status = "planned" },
        new Reservation { Id = 5, RoomId = 5, OrganizerName = "Tomasz Kamiński", Topic = "Hackathon IoT", Date = new DateOnly(2026, 5, 15), StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(16, 0), Status = "confirmed" },
        new Reservation { Id = 6, RoomId = 2, OrganizerName = "Ewa Lewandowska", Topic = "Szkolenie Docker", Date = new DateOnly(2026, 5, 10), StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(16, 0), Status = "cancelled" }
    };
}
