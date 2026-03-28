using BBNVReportCard.Services;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Load local overrides (gitignored — safe for real connection strings / secrets)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ReportCardService>();
builder.Services.AddScoped<PrePrimaryService>();

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
