# WebApp.Tests

xUnit tests for the WebApp project: 6 positive and 6 negative tests. They are run by hand during development and are not part of any build or deployment step.

## Running

From the repository root:

```bash
dotnet test WebApp.Tests                               # all 12 tests
dotnet test WebApp.Tests --filter Category=Positive    # the 6 positive tests
dotnet test WebApp.Tests --filter Category=Negative    # the 6 negative tests
```

In VS Code, the tests also appear in the Testing panel (C# Dev Kit).

## What is tested

| File | Tests | Type |
|---|---|---|
| `CodeAnswerCheckerTests.cs` | Grading of fill-in-the-blank code questions | Unit |
| `PasswordRulesTests.cs` | Password length rules | Unit |
| `QuestFormMapperTests.cs` | Turning the quest form into database rows | Unit |
| `SignInTests.cs` | Signing in, wrong passwords, and `[Authorize]` on saving quests | Integration |

## How the integration tests work

The integration tests start the real app in memory through `TestWebAppFactory`, which points the app at a temporary SQLite file. On startup the app migrates and seeds it like any other database: 8 demo users with the demo password, and the quests from `WebApp/Tasks`. The file is deleted when the tests finish, so the real `WebApp/SheetsDB.db` is never used.

The tests fill in forms the way a browser does: they load the page first and post back its anti-forgery token.
