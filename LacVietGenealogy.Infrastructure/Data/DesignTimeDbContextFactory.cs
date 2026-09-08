using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using LacVietGenealogy.Core.Interfaces;

namespace LacVietGenealogy.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("../LacVietGenealogy.API/appsettings.json", optional: true)
            .AddJsonFile("../LacVietGenealogy.API/appsettings.Development.json", optional: true)
            .AddJsonFile("C:/Users/ADMIN/Documents/LacVietGiaPha/LacVietGiaPha-BE/LacVietGenealogy.API/appsettings.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Server=127.0.0.1;Port=3306;Database=lacviet_giapha;User=root;Password=123456;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));
        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new AppDbContext(optionsBuilder.Options, new NullCurrentFamilyTreeService());
    }
}

internal class NullCurrentFamilyTreeService : ICurrentFamilyTreeService
{
    public Guid FamilyTreeId => Guid.Empty;
}