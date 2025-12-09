using BattleShip.Protos;
using FluentValidation;
using Grpc.Core;

namespace BattleShip.API.Services;

public class GrpcGameService : Game.GameBase
{
    private readonly IGameService _gameService;
    private readonly IValidator<GrpcAttackRequest> _attackValidator;
    private readonly IValidator<GrpcUndoRequest> _undoValidator;
    private readonly IValidator<GrpcCreateGameRequest> _createGameValidator;
    private readonly IValidator<GrpcPlaceShipsRequest> _placeShipsValidator;
    private readonly IValidator<GrpcUndoToTurnRequest> _undoToTurnValidator;

    public GrpcGameService(
        IGameService gameService, 
        IValidator<GrpcAttackRequest> attackValidator, 
        IValidator<GrpcUndoRequest> undoValidator,
        IValidator<GrpcCreateGameRequest> createGameValidator,
        IValidator<GrpcPlaceShipsRequest> placeShipsValidator,
        IValidator<GrpcUndoToTurnRequest> undoToTurnValidator)
    {
        _gameService = gameService;
        _attackValidator = attackValidator;
        _undoValidator = undoValidator;
        _createGameValidator = createGameValidator;
        _placeShipsValidator = placeShipsValidator;
        _undoToTurnValidator = undoToTurnValidator;
    }

    public override async Task<GrpcGameStatus> CreateGame(GrpcCreateGameRequest request, ServerCallContext context)
    {
        var validationResult = await _createGameValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
             throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        var difficulty = (BattleShip.Models.DifficultyLevel)request.Difficulty;
        int size = request.GridSize > 0 ? request.GridSize : 10;
        
        var game = _gameService.CreateGame(difficulty, size);
        return MapToGrpc(game);
    }

    public override async Task<GrpcGameStatus> Attack(GrpcAttackRequest request, ServerCallContext context)
    {
        var validationResult = await _attackValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
             throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        try
        {
            var gameId = Guid.Parse(request.GameId);
            var game = _gameService.Attack(gameId, request.Row, request.Col);
            return MapToGrpc(game);
        }
        catch (ArgumentException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }
    }

    public override async Task<GrpcGameStatus> Undo(GrpcUndoRequest request, ServerCallContext context)
    {
        var validationResult = await _undoValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
             throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        try
        {
            var gameId = Guid.Parse(request.GameId);
            var game = _gameService.Undo(gameId);
            return MapToGrpc(game);
        }
        catch (ArgumentException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }
    }

    public override async Task<GrpcGameStatus> UndoToTurn(GrpcUndoToTurnRequest request, ServerCallContext context)
    {
        var validationResult = await _undoToTurnValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
             throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        try
        {
            var gameId = Guid.Parse(request.GameId);
            var game = _gameService.UndoToTurn(gameId, request.Turn);
            return MapToGrpc(game);
        }
        catch (ArgumentException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Game not found"));
        }
    }

    public override async Task<GrpcGameStatus> PlaceShips(GrpcPlaceShipsRequest request, ServerCallContext context)
    {
        var validationResult = await _placeShipsValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
             throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        try
        {
            var gameId = Guid.Parse(request.GameId);
            var ships = request.Ships.Select(s => new BattleShip.Models.ShipInfo
            {
                Letter = s.Letter.FirstOrDefault(),
                Size = s.Size,
                Row = s.Row,
                Col = s.Col,
                IsHorizontal = s.IsHorizontal
            }).ToList();

            var game = _gameService.PlaceShips(gameId, ships);
            return MapToGrpc(game);
        }
        catch (ArgumentException ex)
        {
             if (ex.Message.Contains("not found"))
                throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
             else
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    private GrpcGameStatus MapToGrpc(BattleShip.Models.GameStatus game)
    {
        var grpcGame = new GrpcGameStatus
        {
            GameId = game.GameId ?? "",
            Winner = game.Winner ?? "",
            IsGameOver = game.IsGameOver,
            LastAttackResult = game.LastAttackResult ?? "",
            LastAiAttackResult = game.LastAiAttackResult ?? "",
            State = (int)game.State
        };

        // Map Player Grid (char[][]) to repeated string
        if (game.PlayerGrid != null)
        {
            foreach (var row in game.PlayerGrid)
            {
                // Convert char[] to string
                grpcGame.PlayerGrid.Add(new string(row));
            }
        }

        // Map Opponent Grid (bool?[][]) to repeated OpponentRow
        if (game.OpponentGrid != null)
        {
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
        }

        // Map History
        if (game.History != null)
        {
            foreach (var h in game.History)
            {
                grpcGame.History.Add(new GrpcMoveHistory
                {
                    Turn = h.Turn,
                    PlayerMove = h.PlayerMove ?? "",
                    PlayerResult = h.PlayerResult ?? "",
                    AiMove = h.AiMove ?? "",
                    AiResult = h.AiResult ?? ""
                });
            }
        }

        // Map Ships
        if (game.Ships != null)
        {
            foreach (var ship in game.Ships)
            {
                grpcGame.Ships.Add(new GrpcShipInfo
                {
                    Letter = ship.Letter.ToString(),
                    Size = ship.Size,
                    Row = ship.Row,
                    Col = ship.Col,
                    IsHorizontal = ship.IsHorizontal
                });
            }
        }

        return grpcGame;
    }
}
