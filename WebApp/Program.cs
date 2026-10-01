using Microsoft.EntityFrameworkCore;
using SheetsApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<WebApplication1.Data.SheetsDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SheetsDBContextConnection")));

var app = builder.Build();



if (!app.Environment.IsDevelopment()) 
{
    app.UseExceptionHandler("/Home/Error"); 
    app.UseHsts();
} 
// This middleware will handle exceptions and redirect to the    
// Error action in the Home controller as long as the application is not in development mode.

app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}"); 
// This middleware will handle status codes and redirect
//  to the StatusCode action in the Home controller.

app.Run();
