using System.ComponentModel.DataAnnotations;

namespace TrainingCenter.Models;

public class Room
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa sali jest wymagana.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kod budynku jest wymagany.")]
    public string BuildingCode { get; set; } = string.Empty;

    public int Floor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Pojemność musi być większa od zera.")]
    public int Capacity { get; set; }

    public bool HasProjector { get; set; }

    public bool IsActive { get; set; }
}
