using BattleShip.API.Services;
using BattleShip.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IGameService, GameService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapGet("/", () => "BattleShip API is running!");

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

