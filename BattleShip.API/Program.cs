using BattleShip.API.Services;
using BattleShip.API.Validators;
using BattleShip.Models;
using BattleShip.Protos;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IAiService, AiService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddScoped<IValidator<AttackRequest>, AttackRequestValidator>();
builder.Services.AddScoped<IValidator<GrpcAttackRequest>, GrpcAttackRequestValidator>();
builder.Services.AddScoped<IValidator<GrpcUndoRequest>, GrpcUndoRequestValidator>();
builder.Services.AddGrpc(); // Add gRPC services

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding"); // Required for gRPC-Web
        });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseGrpcWeb(); // Enable gRPC-Web

app.MapGet("/", () => "BattleShip API is running!");

// Map gRPC Service
app.MapGrpcService<GrpcGameService>().EnableGrpcWeb();

app.MapPost("/game", Ok<GameStatus> (CreateGameRequest request, IGameService gameService) =>
{
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
        var game = gameService.Attack(id, request.Row, request.Col, request.GridSize);
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

app.MapPost("/game/{id}/undo-to-turn/{turn}", Results<Ok<GameStatus>, NotFound<string>> (Guid id, int turn, IGameService gameService) =>
{
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

app.Run();

