using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Data;
using NoteTrackerApp.Seeds;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ── Veritabanı bağlantısı: Render'da DATABASE_URL (PostgreSQL), lokalde MySQL ──
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Render.com: DATABASE_URL ortam değişkeni varsa PostgreSQL kullan
    // Render'ın postgres:// URL'ini Npgsql formatına çevir
    var uri = new Uri(databaseUrl);
    var host = uri.Host;
    var port = uri.Port;
    var db = uri.AbsolutePath.TrimStart('/');
    var user = uri.UserInfo.Split(':')[0];
    var password = uri.UserInfo.Split(':')[1];
    var pgConnStr = $"Host={host};Port={port};Database={db};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(pgConnStr));
}
else
{
    // Lokal geliştirme: appsettings.json'daki MySQL bağlantısı
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
    });

var app = builder.Build();

// Auto-migrate + Auto-seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NoteTrackerApp.Data.AppDbContext>();
    db.Database.Migrate();
    await StudentSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Render HTTP üzerinde çalışır, HTTPS yönlendirmesini kapat
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL")))
{
    // Render ortamında HTTPS redirect kapalı (Render kendi halleder)
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
