using System.ComponentModel.DataAnnotations;

namespace TrainingCenter.Models;

public class Reservation : IValidatableObject
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    [Required(ErrorMessage = "Nazwa organizatora jest wymagana.")]
    public string OrganizerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Temat jest wymagany.")]
    public string Topic { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Status jest wymagany.")]
    public string Status { get; set; } = string.Empty; // planned, confirmed, cancelled

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia.",
                new[] { nameof(EndTime) });
        }
    }
}
