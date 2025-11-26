using BattleShip.API.Services;
using BattleShip.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IGameService, GameService>();
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

app.MapPost("/game", (IGameService gameService) =>
{
    var game = gameService.CreateGame();
    return Results.Ok(game);
});

app.MapPost("/game/{id}/attack", (Guid id, AttackRequest request, IGameService gameService) =>
{
    try
    {
        var game = gameService.Attack(id, request.Row, request.Col);
        return Results.Ok(game);
    }
    catch (ArgumentException)
    {
        return Results.NotFound("Game not found");
    }
});

app.Run();

