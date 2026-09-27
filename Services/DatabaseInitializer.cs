using Dapper;
using Npgsql;

namespace TestJob.Services;

public class DatabaseInitializer
{
    private readonly string _connectionString;
    public DatabaseInitializer(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' not found.");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        const string sql = """
            CREATE TABLE IF NOT EXISTS elements
            (
                id BIGSERIAL PRIMARY KEY,
                attribute_value TEXT,
                element_html TEXT NOT NULL
            );
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
