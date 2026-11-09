using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace WebApi_IntegrationTests;

// Inicia una instancia temporal de SQL Server para las pruebas de integración.
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // Se inicia SQL Server dentro de Docker y se crea una base exclusiva de pruebas.
        await _container.StartAsync();

        const string databaseName = "ReservasPruebas";
        var masterConnection = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "master"
        };

        await using (var connection = new SqlConnection(masterConnection.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}]", connection);
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        // Se crean únicamente las tablas que utiliza este escenario.
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "ScriptPruebaReserva.sql");
        var schema = await File.ReadAllTextAsync(scriptPath, TestContext.Current.CancellationToken);

        await using var databaseConnection = new SqlConnection(ConnectionString);
        await databaseConnection.OpenAsync(TestContext.Current.CancellationToken);
        await using var schemaCommand = new SqlCommand(schema, databaseConnection);
        await schemaCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        // El contenedor y la base temporal se eliminan al terminar las pruebas.
        await _container.DisposeAsync();
    }
}
