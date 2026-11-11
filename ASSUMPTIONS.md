# Assumptions

- Student identity is the 17-digit birth registration number sent in `X-Student-Id`; no authentication is implemented because the assignment requires no login.
- Admin and student areas are separate views in the same frontend application.
- Each class/shift is an independent class row with its own capacity.
- A hold lasts `HOLD_DURATION_SECONDS` (default 600 seconds). The server clock in UTC decides expiry.
- Confirming an active hold simulates successful admission-fee payment; no real gateway is connected.
- Only HELD applications can be cancelled. Confirmed applications cannot be cancelled.
- A student can have one HELD or CONFIRMED application per class.
- PostgreSQL row locks on the class record serialize new seat holds for the same class. A partial unique index prevents duplicate active applications per student/class.
- Expired holds are updated during class counts, application listing, and hold reads; their seats are not counted even if no student opens the app.
- The student ID is saved in localStorage for convenience; application data is always retrieved from the API.
- The admin view is intentionally not authenticated, matching the assignment's no-login requirement.
- Default database credentials are local demonstration credentials only.
