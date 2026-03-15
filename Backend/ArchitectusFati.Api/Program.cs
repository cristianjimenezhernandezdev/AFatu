using ArchitectusFati.Api.Configuration;
using ArchitectusFati.Api.Contracts;
using ArchitectusFati.Api.Data;
using ArchitectusFati.Api.Hosting;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy("UnityClient", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton(sp =>
{
    string? configuredConnectionString =
        builder.Configuration.GetConnectionString("Neon") ??
        Environment.GetEnvironmentVariable("ARCHITECTUSFATI_NEON_CONNECTION_STRING");

    if (string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        throw new InvalidOperationException(
            "No s'ha configurat cap connection string de Neon. Usa ConnectionStrings:Neon o ARCHITECTUSFATI_NEON_CONNECTION_STRING.");
    }

    NpgsqlConnectionStringBuilder connectionBuilder = new(configuredConnectionString)
    {
        Pooling = true
    };

    if (connectionBuilder.SslMode is SslMode.Disable or SslMode.Allow or SslMode.Prefer)
    {
        connectionBuilder.SslMode = SslMode.Require;
    }

    return new NpgsqlDataSourceBuilder(connectionBuilder.ConnectionString).Build();
});

builder.Services.AddSingleton<GameRepository>();
builder.Services.AddSingleton<ContentRepository>();
builder.Services.AddSingleton<RunRepository>();
builder.Services.AddHostedService<DatabaseInitializerHostedService>();

var app = builder.Build();

app.UseCors("UnityClient");

app.MapGet("/api/health", async (GameRepository repository, CancellationToken cancellationToken) =>
{
    bool databaseReachable = await repository.CanConnectAsync(cancellationToken);
    return Results.Ok(new HealthResponse("ok", DateTimeOffset.UtcNow, databaseReachable));
});

app.MapGet("/api/content/bootstrap", async (ContentRepository repository, CancellationToken cancellationToken) =>
{
    BootstrapContentResponse content = await repository.GetBootstrapContentAsync(cancellationToken);
    return Results.Ok(content);
});

app.MapGet("/api/cards", async (ContentRepository repository, CancellationToken cancellationToken) =>
{
    IReadOnlyList<CardDefinition> cards = await repository.GetActiveCardsAsync(cancellationToken);
    return Results.Ok(new CardsResponse(cards));
});

app.MapGet("/api/players/{playerId}/progress", async (string playerId, GameRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(playerId))
        return Results.BadRequest(new ErrorResponse("player_id_required", "Cal indicar un playerId valid."));

    PlayerProgressDto? progress = await repository.GetProgressAsync(playerId, cancellationToken);
    return progress is null
        ? Results.NotFound(new ErrorResponse("player_not_found", "No hi ha progres guardat per aquest playerId."))
        : Results.Ok(new PlayerProgressResponse(playerId, progress));
});

app.MapPut("/api/players/{playerId}/progress", async (
    string playerId,
    UpsertPlayerProgressRequest request,
    GameRepository repository,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(playerId))
        return Results.BadRequest(new ErrorResponse("player_id_required", "Cal indicar un playerId valid."));

    if (request?.Progress is null)
        return Results.BadRequest(new ErrorResponse("progress_required", "El body ha d'incloure l'objecte progress."));

    PlayerProgressDto savedProgress = await repository.UpsertProgressAsync(playerId, request.Progress, cancellationToken);
    return Results.Ok(new PlayerProgressResponse(playerId, savedProgress));
});

app.MapPost("/api/players/{playerId}/runs", async (
    string playerId,
    StartRunRequest request,
    RunRepository repository,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(playerId))
        return Results.BadRequest(new ErrorResponse("player_id_required", "Cal indicar un playerId valid."));

    RunSessionDto run = await repository.StartRunAsync(playerId, request, cancellationToken);
    return Results.Ok(new RunSessionResponse(run));
});

app.MapGet("/api/players/{playerId}/runs/active", async (string playerId, RunRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(playerId))
        return Results.BadRequest(new ErrorResponse("player_id_required", "Cal indicar un playerId valid."));

    RunSessionDto? run = await repository.GetActiveRunAsync(playerId, cancellationToken);
    return run is null
        ? Results.NotFound(new ErrorResponse("run_not_found", "No hi ha cap run activa per aquest playerId."))
        : Results.Ok(new RunSessionResponse(run));
});

app.MapGet("/api/runs/{runId:long}", async (long runId, RunRepository repository, CancellationToken cancellationToken) =>
{
    RunSessionDto? run = await repository.GetRunAsync(runId, cancellationToken);
    return run is null
        ? Results.NotFound(new ErrorResponse("run_not_found", "No existeix aquesta run."))
        : Results.Ok(new RunSessionResponse(run));
});

app.MapPost("/api/runs/{runId:long}/segments", async (
    long runId,
    UpsertRunSegmentRequest request,
    RunRepository repository,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.CardId) || string.IsNullOrWhiteSpace(request.BiomeId))
        return Results.BadRequest(new ErrorResponse("segment_invalid", "CardId i BiomeId son obligatoris."));

    RunSegmentDto? segment = await repository.UpsertRunSegmentAsync(runId, request, cancellationToken);
    return segment is null
        ? Results.NotFound(new ErrorResponse("run_not_found", "No s'ha pogut desar el segment perque la run no existeix."))
        : Results.Ok(new RunSegmentResponse(segment));
});

app.MapPost("/api/runs/{runId:long}/finish", async (
    long runId,
    FinishRunRequest request,
    RunRepository repository,
    CancellationToken cancellationToken) =>
{
    RunSessionDto? run = await repository.FinishRunAsync(runId, request, cancellationToken);
    return run is null
        ? Results.NotFound(new ErrorResponse("run_not_found", "No existeix aquesta run."))
        : Results.Ok(new RunSessionResponse(run));
});

app.Run();
