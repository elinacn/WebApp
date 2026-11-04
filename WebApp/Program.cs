using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApp.Extensions;
using WebApp.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<WebApp.Models.SheetsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SheetsDBContextConnection")));

// Sign-in: a login cookie identifies the user on every request (see AccountController)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";   // where [Authorize] sends anonymous visitors
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Sign out anyone whose user has been deleted since they signed in
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.GetUserId();
            var db = context.HttpContext.RequestServices.GetRequiredService<SheetsDbContext>();
            if (userId is null || !await db.Users.AnyAsync(u => u.Id == userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SheetsDbContext>();
    context.Database.Migrate();
    UserSeeder.Seed(context, app.Logger);
    QuestSeeder.Seed(context, Path.Combine(app.Environment.ContentRootPath, "Tasks"), app.Logger);
}

// This middleware will handle exceptions and redirect to the    
// Error action in the Home controller as long as the application is not in development mode.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// This middleware will handle status codes and redirect
//  to the StatusCode action in the Home controller.
app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}"); 


app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();   // reads the login cookie; must come before UseAuthorization
// This middleware will helps us do testing on user functionality without having to implement a full authentication system yet. 
app.Use(async (context, next) =>
{
    // Check if the user is already authenticated 
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            // This matches the claim name used in _Layout.cshtml for the profile picture
            new Claim("ProfilePicture", "https://via.placeholder.com/150") 
        };

        var identity = new ClaimsIdentity(claims, "MockCookieAuth");
        context.User = new ClaimsPrincipal(identity);
    }

    await next();
});

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();