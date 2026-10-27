using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<WebApp.Models.SheetsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SheetsDBContextConnection")));


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<SheetsDbContext>().Database.Migrate();
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