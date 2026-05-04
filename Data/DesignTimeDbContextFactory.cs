// Bu dosya sadece migration oluşturmak için geçici olarak kullanılır.
// "dotnet ef migrations add InitialPostgres" komutunu çalıştırdıktan sonra silinecek.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NoteTrackerApp.Data;

namespace NoteTrackerApp
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            // PostgreSQL için design-time connection (migration üretimi için)
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=notetracker;Username=postgres;Password=postgres");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
