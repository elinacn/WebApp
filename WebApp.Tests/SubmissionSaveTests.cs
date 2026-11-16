using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using WebApp.Controllers;
using WebApp.Models;
using WebApp.DAL;
using WebApp.Services;
using WebApp.ViewModels;

namespace WebApp.Tests;

public class SubmissionSaveTests
{
    [Fact]
    public async Task Successful_submission_saves_score_and_streak()
    {
        var outcome = await SubmitAsync(null);
        Assert.NotNull(outcome.Model.Result);
        Assert.Null(outcome.Model.ResultError);
        Assert.Null(outcome.Model.StreakError);
        Assert.Equal(10, outcome.Points);
        Assert.Equal(1, outcome.Completions);
        Assert.Equal(1, outcome.Streaks);
    }

    [Fact]
    public async Task Failed_score_save_does_not_attempt_streak_save_and_returns_graded_answers()
    {
        var outcome = await SubmitAsync(typeof(QuestCompletion));
        Assert.Null(outcome.Model.Result);
        Assert.NotNull(outcome.Model.ResultError);
        Assert.True(Assert.Single(outcome.Model.Questions).IsCorrect);
        Assert.Equal(0, outcome.StreakSaveAttempts);
        Assert.Equal(0, outcome.Points);
        Assert.Equal(0, outcome.Completions);
        Assert.Equal(0, outcome.Streaks);
    }

    [Fact]
    public async Task Failed_streak_save_keeps_saved_score_and_returns_warning()
    {
        var outcome = await SubmitAsync(typeof(DailyStreaks));
        Assert.NotNull(outcome.Model.Result);
        Assert.Null(outcome.Model.ResultError);
        Assert.NotNull(outcome.Model.StreakError);
        Assert.True(Assert.Single(outcome.Model.Questions).IsCorrect);
        Assert.Equal(10, outcome.Points);
        Assert.Equal(1, outcome.Completions);
        Assert.Equal(0, outcome.Streaks);
    }

    private static async Task<Outcome> SubmitAsync(Type? failureType)
    {
        var failure = new FailSaveInterceptor(failureType);
        var options = new DbContextOptionsBuilder<SheetsDbContext>()
            .UseSqlite("Data Source=:memory:").AddInterceptors(failure).Options;
        await using var context = new SheetsDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var user = new User { Username = "student", Name = "Student", StudyProgram = "Web" };
        var question = new QuestQuestion
        {
            Type = "multipleChoice", Question = "Choose A", Points = 10, CorrectOptionId = "a",
            Options = [new QuestOption { OptionKey = "a", Text = "A" }, new QuestOption { OptionKey = "b", Text = "B" }]
        };
        var quest = new Quest { Title = "Test quest", Difficulty = "Beginner", Questions = [question] };
        context.AddRange(user, quest);
        await context.SaveChangesAsync();
        failure.Enabled = true;
        var controller = new QuestsController(NullLogger<QuestsController>.Instance, new QuestRepository(context),
            new DailyStreakService(new ProgressRepository(context)),
            new QuestScoringService(new UserRepository(context), new QuestRepository(context), new ProgressRepository(context), NullLogger<QuestScoringService>.Instance));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "Test"))
            }
        };
        var result = Assert.IsType<ViewResult>(await controller.Play(quest.Id,
            [new PlayAnswerInput { QuestionId = question.Id, Selected = ["a"] }]));
        var model = Assert.IsType<PlayQuestViewModel>(result.Model);
        return new Outcome(model,
            await context.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(u => u.Points).SingleAsync(),
            await context.QuestCompletions.CountAsync(), await context.DailyStreaks.CountAsync(), failure.StreakSaveAttempts);
    }

    private sealed record Outcome(PlayQuestViewModel Model, int Points, int Completions, int Streaks, int StreakSaveAttempts);

    // Fail at the database save boundary while exercising the real scoring and streak services.
    private sealed class FailSaveInterceptor(Type? failureType) : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public int StreakSaveAttempts { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context is { } context)
            {
                var changes = context.ChangeTracker.Entries()
                    .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
                if (changes.Any(e => e.Entity is DailyStreaks)) StreakSaveAttempts++;
                if (failureType is not null && changes.Any(e => failureType.IsInstanceOfType(e.Entity)))
                    throw new DbUpdateException("Simulated save failure.");
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
