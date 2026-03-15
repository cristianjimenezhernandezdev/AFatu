using ArchitectusFati.Api.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ArchitectusFati.Api.Hosting;

public sealed class DatabaseInitializerHostedService : IHostedService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly DatabaseOptions _options;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;

    public DatabaseInitializerHostedService(
        IHostEnvironment hostEnvironment,
        IOptions<DatabaseOptions> options,
        NpgsqlDataSource dataSource,
        ILogger<DatabaseInitializerHostedService> logger)
    {
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.ApplySchemaOnStartup)
            return;

        string schemaPath = Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, _options.SchemaScriptPath));
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"No s'ha trobat l'script SQL de bootstrap a {schemaPath}.");
        }

        string sql = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        await using NpgsqlCommand command = _dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Schema de la base de dades aplicat des de {SchemaPath}.", schemaPath);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
