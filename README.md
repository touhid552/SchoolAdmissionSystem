# School Admission System

A full-stack school admission demo built with ASP.NET Core 8, PostgreSQL, React and Docker Compose.

## Run the complete application

Install Docker Desktop, open a terminal in the repository root, then run:

```bash
docker compose up -d --build
```

Open:
- Student portal: http://localhost:3000
- Admin dashboard: http://localhost:3000 (choose Admin dashboard in the sidebar)
- API health: http://localhost:8080/health

The backend applies EF Core migrations automatically at startup. PostgreSQL data is stored in the `pgdata` named volume and survives `docker compose down`. Do not use `docker compose down -v` unless you intend to delete all data.

To stop: `docker compose down`. To inspect logs: `docker compose logs -f backend`.

## Features

- Admin can create separate classes/shifts and seat capacities.
- Student identity uses a 17-digit birth registration number; no login is required by the assignment.
- Students can hold a seat for `HOLD_DURATION_SECONDS` (default 600 seconds), simulate payment by confirming, or cancel a held application.
- Expired holds release seats and are excluded from counts.
- Application history and status are loaded from PostgreSQL.
- Seat counts refresh every five seconds.

## API

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/health` | Health status |
| POST | `/classes` | Create a class (`name`, `totalSeats`) |
| GET | `/classes` | List classes and seat counts |
| GET | `/classes/{classId}` | One class and seat counts |
| POST | `/classes/{classId}/holds` | Hold a seat; requires `X-Student-Id` |
| GET | `/holds/{holdId}` | Read a hold |
| POST | `/holds/{holdId}/confirm` | Confirm a hold; requires `X-Student-Id` |
| DELETE | `/holds/{holdId}` | Cancel a held application; requires `X-Student-Id` |
| GET | `/me/holds` | List current student's applications; requires `X-Student-Id` |

## Configuration

Copy `.env.example` to `.env` if you need custom settings. Defaults are suitable for local Docker demonstration. `HOLD_DURATION_SECONDS` controls the hold duration. The frontend API URL is configured with `VITE_API_BASE_URL` at build time.

## Development

Backend: `cd backend && dotnet restore && dotnet build`.
Frontend: `cd frontend && npm install && npm run build`.
Database migrations are in `backend/Migrations` and are applied automatically by the backend container.

## Tests

The repository currently contains the API implementation but does not yet include the full automated integration/concurrency test suite. Verify the main flow through the browser UI. Do not treat a successful `/health` response as proof that all admission workflows pass.

## Known limitations

- This is an assignment demo: student identity is a user-entered 17-digit identifier, not verified authentication.
- Admin UI is not protected by a login because the assignment explicitly requires no login.
- Payment is simulated; no real payment gateway is connected.
