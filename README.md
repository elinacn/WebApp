# WebApp

## Project checklist:
- The project must contain two sub-applications based on the same task.
- Sub-application 1 must use ASP.NET 10.0 MVC for both frontend and backend.
- Sub-application 2 must use ASP.NET 10.0 Web API for the backend and React for the frontend.
- Both sub-applications should be MVPs/proof-of-concept solutions.
- The application should not crash during normal use.
- The application must have a web-based user interface.
- The application must contain at least one interactive form.
- At least one entity must support full CRUD: Create, Read, Update, Delete.
- The group must submit project documentation of 2000–5000 words in English.

## For Task 2 MVP:
- Users can view available quizzes.
- Users can open and complete a quiz.
- Users can select and submit answers.
- Users receive immediate feedback after answering.
- The system calculates a score or points.
- Users can view their quiz result.
- Questions can be created, viewed, edited, and deleted.
- Quiz and question data should be stored in a database.

## Running subapp 2 (ASP.NET 10 Web API + React)

Subapp 2 is independent of subapp 1: it has its own API project and its own
SQLite database (`WebApp.Api/WebAppApi.db`, created and seeded on first start).

1. Start the API (http://localhost:5114):
   ```bash
   dotnet run --project WebApp.Api --launch-profile http
   ```
2. In a second terminal, start the React app (http://localhost:5173):
   ```bash
   cd webapp-react
   npm install
   npm run dev
   ```
3. Open http://localhost:5173. Vite forwards every `/api` request to the API.

Demo accounts (password `quest1234`): `admin` (Admin: can manage quests, courses
and users) and `anna`, `beatrice`, … (students). Delete `WebAppApi.db` to reset the data.

### Where things are

| Requirement | Subapp 2 |
|---|---|
| 3+ entities with CRUD | Quest, Course, User (`WebApp.Api/Controllers`, React pages in `webapp-react/src/pages`) |
| Many-to-many | User ↔ Quest through `QuestCompletion` (`WebApp.Api/Models`) |
| Complex domain rule | `QuestValidator` (question rules) and `QuestScoringService` (XP only on first completion) |
| Multi-parameter search | `GET /api/quests?search=&difficulty=&courseId=&status=&sort=`, filters on the Quests page |
| DTOs | `WebApp.Api/Dtos` |
| Client-side API service layer | `webapp-react/src/services` (only `httpClient.js` calls `fetch`) |
| Validation | Data annotations + `QuestValidator` on the server, matching checks in each React form |
| Error handling and logging | `GlobalExceptionHandler`, `ILogger` in controllers and services, `ErrorMessage` in React |
| Authentication and roles | Cookie auth in `Program.cs`, `[Authorize(Roles = "Admin")]`, `ProtectedRoute` in React |