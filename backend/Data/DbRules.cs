using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

// The SQL text of the database rules. AppDbContext puts these into the migration,
// so PostgreSQL itself enforces them even if the C# code has a bug.
public static class DbRules
{
    // A class must have at least one seat.
    public const string TotalSeatsPositive = "\"TotalSeats\" > 0";

    // A student ID is exactly 17 digits (PostgreSQL regular expression).
    public const string StudentIdHas17Digits = "\"StudentId\" ~ '^[0-9]{17}$'";

    // Status must be one of the four allowed values.
    public const string StatusIsAllowed =
        $"\"Status\" IN ('{HoldStatus.Held}', '{HoldStatus.Confirmed}', '{HoldStatus.Expired}', '{HoldStatus.Cancelled}')";

    // "Active" holds. The unique index below only looks at rows that match this,
    // so a student can apply again after a hold is EXPIRED or CANCELLED.
    public const string ActiveHoldFilter =
        $"\"Status\" IN ('{HoldStatus.Held}', '{HoldStatus.Confirmed}')";
}
