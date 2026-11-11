namespace SchoolAdmission.Api.Models;

// The four allowed values of Hold.Status.
// Plain strings are used instead of a C# enum so that the database, the C# code
// and the JSON API all use exactly the same text (for example "HELD").
public static class HoldStatus
{
    public const string Held = "HELD";
    public const string Confirmed = "CONFIRMED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";
}
