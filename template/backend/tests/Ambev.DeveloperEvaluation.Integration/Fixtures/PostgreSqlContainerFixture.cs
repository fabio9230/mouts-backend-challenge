using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;
namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:13-alpine")
        .WithDatabase("developer_evaluation_integration_tests")
        .WithUsername("developer")
        .WithPassword("ev@luAt10n")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE EXTENSION IF NOT EXISTS pgcrypto;";
            await command.ExecuteNonQueryAsync();
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public DefaultContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(ConnectionString, options =>
                options.MigrationsAssembly(typeof(DefaultContext).Assembly.FullName))
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .LogTo(
                Console.WriteLine,
                LogLevel.Debug)
            .Options;

        return new DefaultContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "PostgreSQL integration collection";
}
