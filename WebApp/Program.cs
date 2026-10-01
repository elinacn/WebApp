using Microsoft.EntityFrameworkCore;
using SheetsApp.Data;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<WebApplication1.Data.SheetsDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SheetsDBContextConnection")));


var app = builder.Build();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();