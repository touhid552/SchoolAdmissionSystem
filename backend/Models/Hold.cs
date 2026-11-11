namespace SchoolAdmission.Api.Models;

// One student's application for one class. It holds a seat for a limited time.
// Stored in the "Holds" table.
public class Hold
{
    // Random text such as "h_81bd..." (created in code in a later task).
    public string Id { get; set; } = string.Empty;

    // The class this application is for (foreign key to Classes.Id).
    public string ClassId { get; set; } = string.Empty;

    // The 17-digit birth registration number (checked by the database as well).
    public string StudentId { get; set; } = string.Empty;

    // One of the values in HoldStatus.
    public string Status { get; set; } = HoldStatus.Held;

    // Both times are always UTC.
    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
