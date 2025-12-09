using BattleShip.API.Services;
using BattleShip.API.Validators;
using BattleShip.Models;
using BattleShip.Protos;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using BattleShip.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IAiService, AiService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IValidator<AttackRequest>, AttackRequestValidator>();
builder.Services.AddScoped<IValidator<CreateGameRequest>, CreateGameRequestValidator>();
builder.Services.AddScoped<IValidator<PlaceShipsRequest>, PlaceShipsRequestValidator>();
builder.Services.AddScoped<IValidator<GrpcAttackRequest>, GrpcAttackRequestValidator>();
builder.Services.AddScoped<IValidator<GrpcUndoRequest>, GrpcUndoRequestValidator>();
builder.Services.AddScoped<IValidator<GrpcCreateGameRequest>, GrpcCreateGameRequestValidator>();
builder.Services.AddScoped<IValidator<GrpcPlaceShipsRequest>, GrpcPlaceShipsRequestValidator>();
builder.Services.AddScoped<IValidator<GrpcUndoToTurnRequest>, GrpcUndoToTurnRequestValidator>();
builder.Services.AddGrpc(); // Add gRPC services
builder.Services.AddSignalR(); // Add SignalR

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Blazor app address
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials() // Required for SignalR
                   .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding"); // Required for gRPC-Web
        });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseGrpcWeb(); // Enable gRPC-Web

app.MapGet("/", () => "BattleShip API is running!");

// Map gRPC Service
app.MapGrpcService<GrpcGameService>().EnableGrpcWeb();
app.MapHub<GameHub>("/gamehub"); // Map SignalR Hub

app.MapGet("/leaderboard", async (ILeaderboardService leaderboardService) =>
{
    return Results.Ok(await leaderboardService.GetLeaderboardAsync());
});

app.MapPost("/game", async Task<Results<Ok<GameStatus>, ValidationProblem>> (CreateGameRequest request, IGameService gameService, IValidator<CreateGameRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return TypedResults.ValidationProblem(validationResult.ToDictionary());
    }
    var game = gameService.CreateGame(request.Difficulty, request.GridSize);
    return TypedResults.Ok(game);
});

app.MapPost("/game/{id}/attack", async Task<Results<Ok<GameStatus>, NotFound<string>, ValidationProblem>> (Guid id, AttackRequest request, IGameService gameService, IValidator<AttackRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return TypedResults.ValidationProblem(validationResult.ToDictionary());
    }

    try
    {
        var game = gameService.Attack(id, request.Row, request.Col);
        return TypedResults.Ok(game);
    }
    catch (ArgumentException)
    {
        return TypedResults.NotFound("Game not found");
    }
});

app.MapPost("/game/{id}/undo", Results<Ok<GameStatus>, NotFound<string>> (Guid id, IGameService gameService) =>
{
    try
    {
        var game = gameService.Undo(id);
        return TypedResults.Ok(game);
    }
    catch (ArgumentException)
    {
        return TypedResults.NotFound("Game not found");
    }
});

app.MapPost("/game/{id}/undo-to-turn/{turn}", Results<Ok<GameStatus>, NotFound<string>, BadRequest<string>> (Guid id, int turn, IGameService gameService) =>
{
    if (turn <= 0) return TypedResults.BadRequest("Turn must be greater than 0.");
    try
    {
        var game = gameService.UndoToTurn(id, turn);
        return TypedResults.Ok(game);
    }
    catch (ArgumentException)
    {
        return TypedResults.NotFound("Game not found");
    }
});

app.MapPost("/game/{id}/place-ships", async Task<Results<Ok<GameStatus>, BadRequest<string>, ValidationProblem>> (Guid id, PlaceShipsRequest request, IGameService gameService, IValidator<PlaceShipsRequest> validator) =>
{
    if (id != request.GameId) return TypedResults.BadRequest("Game ID mismatch");
    
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return TypedResults.ValidationProblem(validationResult.ToDictionary());
    }

    try
    {
        var status = gameService.PlaceShips(id, request.Ships);
        return TypedResults.Ok(status);
    }
    catch (ArgumentException ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
});

app.Run();
