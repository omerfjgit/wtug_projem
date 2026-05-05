// Bu dosya sadece migration oluşturmak için geçici olarak kullanılır.
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
            // MySQL için design-time connection (migration üretimi için)
            var connectionString = "Server=localhost;Database=NoteTrackerDB;User=root;Password=1234;";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
