using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Models;

namespace WebApp.Tests;

public class RoleAuthorizationTests
{
    [Fact]
    public async Task Student_cannot_use_admin_routes_or_see_admin_navigation()
    {
        using var factory = new TestWebAppFactory();
        using var browser = factory.CreateBrowser();
        await TestWebAppFactory.SignInAsync(browser, "anna", UserSeeder.DemoPassword);
        foreach (var url in new[] { "/Users", "/Users/Details/1", "/Users/Create", "/Users/Delete/1",
            "/Courses/Create", "/Courses/Edit/1", "/Courses/Delete/1", "/Quests/Delete/1" })
            Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync(url)).StatusCode);
        foreach (var url in new[] { "/Users/Create", "/Users/Delete/1", "/Courses/Create", "/Courses/Edit/1", "/Courses/Delete/1", "/Quests/Delete/1" })
            Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(browser, "/Account/Login", url, [])).StatusCode);
        var home = await browser.GetStringAsync("/");
        Assert.DoesNotContain(">Users</a>", home);
        var library = await browser.GetStringAsync("/Quests/Quests");
        Assert.DoesNotContain("/Quests/Delete/", library);
    }

    [Fact]
    public async Task Student_can_create_and_edit_own_quest_but_cannot_edit_anothers_or_change_creator()
    {
        using var factory = new TestWebAppFactory();
        using var anna = factory.CreateBrowser();
        using var beatrice = factory.CreateBrowser();
        await TestWebAppFactory.SignInAsync(anna, "anna", UserSeeder.DemoPassword);
        await TestWebAppFactory.SignInAsync(beatrice, "beatrice", UserSeeder.DemoPassword);
        int annaId, beatriceId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetsDbContext>();
            annaId = (await db.Users.SingleAsync(u => u.Username == "anna")).Id;
            beatriceId = (await db.Users.SingleAsync(u => u.Username == "beatrice")).Id;
        }
        var form = QuestForm("Owned quest");
        form["NewQuest.CreatorUserId"] = beatriceId.ToString();
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(anna, "/Quests/Quests?section=create", "/Quests/Create", form)).StatusCode);
        int questId;
        using (var scope = factory.Services.CreateScope())
        {
            var quest = await scope.ServiceProvider.GetRequiredService<SheetsDbContext>().Quests.SingleAsync(q => q.Title == "Owned quest");
            questId = quest.Id;
            Assert.Equal(annaId, quest.CreatorUserId);
        }
        Assert.Equal(HttpStatusCode.OK, (await anna.GetAsync($"/Quests/Edit/{questId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await beatrice.GetAsync($"/Quests/Edit/{questId}")).StatusCode);
        form["NewQuest.Id"] = questId.ToString();
        form["NewQuest.Title"] = "Unauthorized edit";
        form["addQuestion"] = "0";
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(beatrice, "/Quests/Quests?section=create", $"/Quests/Edit/{questId}", form)).StatusCode);
        form.Remove("addQuestion");
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(beatrice, "/Quests/Quests?section=create", $"/Quests/Edit/{questId}", form)).StatusCode);
        form["NewQuest.Title"] = "Owner updated quest";
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(anna, $"/Quests/Edit/{questId}", $"/Quests/Edit/{questId}", form)).StatusCode);
        using var verify = factory.Services.CreateScope();
        var saved = await verify.ServiceProvider.GetRequiredService<SheetsDbContext>().Quests.SingleAsync(q => q.Id == questId);
        Assert.Equal("Owner updated quest", saved.Title);
        Assert.Equal(annaId, saved.CreatorUserId);
        Assert.DoesNotContain("Owner updated quest", await beatrice.GetStringAsync("/Quests/Quests"));
        Assert.Contains("Owner updated quest", await beatrice.GetStringAsync("/Quests/Quests?section=explore"));
    }

    [Fact]
    public async Task Admin_can_edit_another_users_quest_and_delete_it()
    {
        using var factory = new TestWebAppFactory();
        using var browser = factory.CreateBrowser();
        int questId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetsDbContext>();
            (await db.Users.SingleAsync(u => u.Username == "anna")).IsAdmin = true;
            var owner = await db.Users.SingleAsync(u => u.Username == "beatrice");
            var quest = new Quest { Title = "Other student quest", Difficulty = "Beginner", CreatorUserId = owner.Id };
            db.Quests.Add(quest);
            await db.SaveChangesAsync();
            questId = quest.Id;
        }
        await TestWebAppFactory.SignInAsync(browser, "anna", UserSeeder.DemoPassword);
        var form = QuestForm("Admin updated quest");
        form["NewQuest.Id"] = questId.ToString();
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(browser, $"/Quests/Edit/{questId}", $"/Quests/Edit/{questId}", form)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(browser, $"/Quests/Delete/{questId}", $"/Quests/Delete/{questId}", [])).StatusCode);
        using var verify = factory.Services.CreateScope();
        Assert.False(await verify.ServiceProvider.GetRequiredService<SheetsDbContext>().Quests.AnyAsync(q => q.Id == questId));
    }

    [Fact]
    public async Task Student_cannot_promote_self_by_posting_an_admin_field()
    {
        using var factory = new TestWebAppFactory();
        using var browser = factory.CreateBrowser();
        await TestWebAppFactory.SignInAsync(browser, "anna", UserSeeder.DemoPassword);
        int id;
        using (var scope = factory.Services.CreateScope())
            id = (await scope.ServiceProvider.GetRequiredService<SheetsDbContext>().Users.SingleAsync(u => u.Username == "anna")).Id;
        var form = new Dictionary<string, string>
        {
            ["Id"] = id.ToString(), ["Username"] = "anna", ["Name"] = "Anna", ["StudyProgram"] = "Web", ["IsAdmin"] = "true"
        };
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(browser, $"/Users/Edit/{id}", $"/Users/Edit/{id}", form)).StatusCode);
        using var verify = factory.Services.CreateScope();
        Assert.False((await verify.ServiceProvider.GetRequiredService<SheetsDbContext>().Users.FindAsync(id))!.IsAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/Users")).StatusCode);
    }

    [Fact]
    public async Task Role_changes_apply_to_already_signed_in_sessions()
    {
        using var factory = new TestWebAppFactory();
        using var browser = factory.CreateBrowser();
        await TestWebAppFactory.SignInAsync(browser, "anna", UserSeeder.DemoPassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/Users")).StatusCode);
        async Task SetRoleAsync(bool admin)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SheetsDbContext>();
            (await db.Users.SingleAsync(u => u.Username == "anna")).IsAdmin = admin;
            await db.SaveChangesAsync();
        }
        await SetRoleAsync(true);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/Users")).StatusCode);
        Assert.Contains(">Users</a>", await browser.GetStringAsync("/"));
        await SetRoleAsync(false);
        Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/Users")).StatusCode);
        Assert.DoesNotContain(">Users</a>", await browser.GetStringAsync("/"));
    }

    [Fact]
    public async Task Last_admin_cannot_be_demoted_or_deleted()
    {
        using var factory = new TestWebAppFactory();
        using var browser = factory.CreateBrowser();
        int id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetsDbContext>();
            var admin = await db.Users.SingleAsync(u => u.Username == "anna");
            admin.IsAdmin = true;
            await db.SaveChangesAsync();
            id = admin.Id;
        }
        await TestWebAppFactory.SignInAsync(browser, "anna", UserSeeder.DemoPassword);
        var demote = await PostAsync(browser, $"/Users/Edit/{id}", $"/Users/Edit/{id}", new()
        {
            ["Id"] = id.ToString(), ["Username"] = "anna", ["Name"] = "Anna", ["StudyProgram"] = "Web", ["IsAdmin"] = "false"
        });
        Assert.Equal(HttpStatusCode.OK, demote.StatusCode);
        Assert.Contains("Keep at least one admin account.", await demote.Content.ReadAsStringAsync());
        await PostAsync(browser, $"/Users/Delete/{id}", $"/Users/Delete/{id}", []);
        using var verify = factory.Services.CreateScope();
        Assert.True((await verify.ServiceProvider.GetRequiredService<SheetsDbContext>().Users.FindAsync(id))!.IsAdmin);
    }

    private static Dictionary<string, string> QuestForm(string title) => new()
    {
        ["NewQuest.Title"] = title, ["NewQuest.Difficulty"] = "Beginner", ["NewQuest.Questions[0].Type"] = "freeText",
        ["NewQuest.Questions[0].Question"] = "Say yes", ["NewQuest.Questions[0].Points"] = "1",
        ["NewQuest.Questions[0].AcceptedAnswers[0]"] = "yes"
    };

    private static async Task<HttpResponseMessage> PostAsync(HttpClient browser, string page, string action, Dictionary<string, string> form)
    {
        form["__RequestVerificationToken"] = await TestWebAppFactory.GetAntiForgeryTokenAsync(browser, page);
        return await browser.PostAsync(action, new FormUrlEncodedContent(form));
    }
}
