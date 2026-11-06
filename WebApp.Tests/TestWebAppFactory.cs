using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace WebApp.Tests;

// Starts the real app in memory for integration tests. Each factory gets its own
// throwaway SQLite file, which Program.cs migrates and seeds (demo users and quests)
// exactly as it does on startup, so the real SheetsDB.db is never touched.
public sealed partial class TestWebAppFactory : WebApplicationFactory<Program>
{
    public string DatabasePath { get; } =
        Path.Combine(Path.GetTempPath(), $"webapp-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:SheetsDBContextConnection", $"Data Source={DatabasePath}");
    }

    // A client that keeps cookies (so sign-in sticks) but doesn't follow redirects,
    // so tests can check where the app sends the user
    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    // Forms are protected against cross-site requests, so tests first load the page
    // and post back the anti-forgery token it contains, like a browser would
    public static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = TokenField().Match(html);
        Assert.True(match.Success, $"No anti-forgery token on {url}");
        return match.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> SignInAsync(HttpClient client, string username, string password)
    {
        var token = await GetAntiForgeryTokenAsync(client, "/Account/Login");
        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["username"] = username,
            ["password"] = password
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { DatabasePath, DatabasePath + "-shm", DatabasePath + "-wal" })
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenField();
}
