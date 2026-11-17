using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.DAL;
using WebApp.Api.Extensions;
using WebApp.Api.Middleware;
using WebApp.Api.Models;
using WebApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Actions check ModelState themselves, so the basic checks (data annotations) and the
// domain rules are returned together in one response instead of in two rounds
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);
builder.Services.AddOpenApi();

// Errors are returned as JSON ProblemDetails; unhandled exceptions are logged by GlobalExceptionHandler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IQuestRepository, QuestRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
builder.Services.AddSingleton<QuestGradingService>();
builder.Services.AddScoped<QuestScoringService>();
builder.Services.AddScoped<StreakService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Sign-in: an HttpOnly cookie identifies the user on every request. The React app is
// served through the Vite proxy on the same origin, so the browser sends the cookie automatically.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "WebApp.Api.Auth";
        options.Cookie.HttpOnly = true;                 // JavaScript can't read the cookie
        options.Cookie.SameSite = SameSiteMode.Strict;  // not sent with requests from other sites (CSRF protection)
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // An API answers with status codes instead of redirecting to a login page;
        // the React app decides what to show for 401 (not signed in) and 403 (not allowed)
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };

        // Sign out anyone whose user was deleted since they signed in
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.GetUserId();
            var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            if (userId is null || !await users.ExistsAsync(userId.Value))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Create/upgrade the database on startup and fill it with demo data the first time
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db, Path.Combine(AppContext.BaseDirectory, "Data", "SeedData"), app.Logger);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
