namespace SchoolAdmission.Api.Models;

// One class-shift, for example "Class 6 - Morning Shift".
// "Class 6 - Morning" and "Class 6 - Day" are two separate rows, each with its own seats.
// Stored in the "Classes" table.
public class SchoolClass
{
    // Random text such as "c_3f9a..." (created in code in a later task).
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    // Must be greater than zero (also enforced by the database).
    public int TotalSeats { get; set; }

    // Always UTC.
    public DateTime CreatedAt { get; set; }
}
