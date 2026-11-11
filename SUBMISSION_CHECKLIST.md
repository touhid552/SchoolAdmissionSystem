# Final submission checklist

1. Extract this project and open its root folder in VS Code.
2. Start Docker Desktop.
3. Run `docker compose up -d --build`.
4. Wait for the backend container to finish starting, then open `http://localhost:3000`.
5. Create a class in Admin dashboard.
6. In Student portal, enter a 17-digit test student ID, apply, and confirm using the simulated payment button.
7. Verify the class seat counts change in Admin dashboard.
8. Run `docker compose logs -f backend` if anything fails.

Before submitting, push the updated files to your public GitHub repository. Do not commit `.env` or the assignment PDF. Keep `AI_USAGE.md` accurate: this project was AI-assisted and final Docker/browser tests must be completed on the developer's machine.

The automated integration/concurrency test suite is not included in this urgent implementation. Do not claim those tests passed.
