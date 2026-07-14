using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccessLayer.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // Used by dotnet ef when DataAccessLayer is the startup project.
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=Prn222RagChatbot;Trusted_Connection=True;TrustServerCertificate=True";
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
