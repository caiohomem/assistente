using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AssistenteExecutivo.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Carregar configuração do appsettings
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AssistenteExecutivo.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Converter URL para formato de parâmetros se necessário
        string finalConnectionString = connectionString;
        if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(connectionString);
                var builder = new Npgsql.NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port != -1 ? uri.Port : 5432,
                    Database = uri.AbsolutePath.TrimStart('/'),
                    Username = uri.UserInfo.Split(':')[0],
                    Password = uri.UserInfo.Split(':').Length > 1 ? uri.UserInfo.Split(':')[1] : "",
                    SslMode = Npgsql.SslMode.Require
                };
                
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    var query = uri.Query.TrimStart('?');
                    var pairs = query.Split('&');
                    foreach (var pair in pairs)
                    {
                        var parts = pair.Split('=');
                        if (parts.Length == 2)
                        {
                            var key = parts[0].ToLowerInvariant();
                            var value = parts[1];
                            
                            if (key == "sslmode")
                            {
                                if (Enum.TryParse<Npgsql.SslMode>(value, true, out var mode))
                                    builder.SslMode = mode;
                            }
                        }
                    }
                }
                
                finalConnectionString = builder.ConnectionString;
            }
            catch
            {
                // Se falhar na conversão, usar a connection string original
            }
        }
        
        optionsBuilder.UseNpgsql(finalConnectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

