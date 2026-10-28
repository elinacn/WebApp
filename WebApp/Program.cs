using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApp.Extensions;
using WebApp.Models;

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
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();