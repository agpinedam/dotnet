using BattleShip.Protos;
using Grpc.Core;

namespace BattleShip.API.Services;

public class GrpcGameService : Game.GameBase
{
    private readonly IGameService _gameService;

    public GrpcGameService(IGameService gameService)
    {
        _gameService = gameService;
    }

    public override Task<GrpcGameStatus> CreateGame(Empty request, ServerCallContext context)
    {
        var game = _gameService.CreateGame();
        return Task.FromResult(MapToGrpc(game));
    }

    public override Task<GrpcGameStatus> Attack(GrpcAttackRequest request, ServerCallContext context)
    {
        try
        {
            var gameId = Guid.Parse(request.GameId);
            var game = _gameService.Attack(gameId, request.Row, request.Col);
            return Task.FromResult(MapToGrpc(game));
        }
        catch (ArgumentException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }
    }

    private GrpcGameStatus MapToGrpc(BattleShip.Models.GameStatus game)
    {
        var grpcGame = new GrpcGameStatus
        {
            GameId = game.GameId.ToString(),
            Winner = game.Winner ?? "",
            IsGameOver = game.IsGameOver,
            LastAttackResult = game.LastAttackResult ?? "",
            LastAiAttackResult = game.LastAiAttackResult ?? ""
        };

        // Map Player Grid (char[][]) to repeated string
        foreach (var row in game.PlayerGrid)
        {
            // Convert char[] to string
            grpcGame.PlayerGrid.Add(new string(row));
        }

        // Map Opponent Grid (bool?[][]) to repeated OpponentRow
        foreach (var row in game.OpponentGrid)
        {
            var grpcRow = new OpponentRow();
            foreach (var cell in row)
            {
                // 0 = null, 1 = hit (true), 2 = miss (false)
                int val = 0;
                if (cell.HasValue)
                {
                    val = cell.Value ? 1 : 2;
                }
                grpcRow.Values.Add(val);
            }
            grpcGame.OpponentGrid.Add(grpcRow);
        }

        return grpcGame;
    }
}
