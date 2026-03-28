using BBNVReportCard.Services;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ReportCardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ReportCard}/{action=Index}/{id?}");

app.Run();
