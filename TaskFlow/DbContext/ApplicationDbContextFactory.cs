using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskFlow.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var projectDirectory = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(projectDirectory, "TaskFlow")))
            projectDirectory = Path.Combine(projectDirectory, "TaskFlow");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection yapılandırması bulunamadı.");
        var databaseProvider = configuration["Database:Provider"] ?? "SqlServer";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            optionsBuilder.UseSqlite(connectionString);
        else
            optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
