namespace SchoolAdmission.Api.Services;

// How long a seat stays held, read from the HOLD_DURATION_SECONDS setting.
// Program.cs checks the value at startup. The hold logic (a later task) reads it from here.
public record HoldSettings(int DurationSeconds);
