using Microsoft.EntityFrameworkCore;
using Shelfora.Data;
using Shelfora.Services;

var seedRequested = args.Contains("--seed", StringComparer.OrdinalIgnoreCase);
var builder = WebApplication.CreateBuilder(args.Where(arg => !string.Equals(arg, "--seed", StringComparison.OrdinalIgnoreCase)).ToArray());
var connectionString = Environment.GetEnvironmentVariable("SHELFORA_SQLSERVER")
    ?? builder.Configuration.GetConnectionString("Shelfora")
    ?? "Server=localhost\\SQLEXPRESS;Database=Shelfora;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<Shelfora.Components.LibraryIdentityViewComponent>();

var app = builder.Build();
if (seedRequested)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program { }
