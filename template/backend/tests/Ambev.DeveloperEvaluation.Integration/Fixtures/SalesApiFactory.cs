using Ambev.DeveloperEvaluation.ORM;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

public sealed class SalesApiFactory : WebApplicationFactory<WebApi.Program>
{
    private readonly string _connectionString;

    public SalesApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DefaultContext>>();

            services.AddDbContext<DefaultContext>(options =>
                options.UseNpgsql(
                    _connectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(DefaultContext).Assembly.FullName))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .LogTo(
                    Console.WriteLine,
                    LogLevel.Debug));
        });
    }
}

