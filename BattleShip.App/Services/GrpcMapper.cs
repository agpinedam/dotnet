using BattleShip.Models;
using BattleShip.Protos;

namespace BattleShip.App.Services;

public static class GrpcMapper
{
    public static GameStatus MapFromGrpc(GrpcGameStatus grpcGame)
    {
        var game = new GameStatus
        {
            GameId = grpcGame.GameId,
            Winner = string.IsNullOrEmpty(grpcGame.Winner) ? null : grpcGame.Winner,
            IsGameOver = grpcGame.IsGameOver,
            LastAttackResult = string.IsNullOrEmpty(grpcGame.LastAttackResult) ? null : grpcGame.LastAttackResult,
            LastAiAttackResult = string.IsNullOrEmpty(grpcGame.LastAiAttackResult) ? null : grpcGame.LastAiAttackResult,
            State = (GameState)grpcGame.State,
            History = new List<MoveHistory>()
        };

        // Map Player Grid
        game.PlayerGrid = new char[grpcGame.PlayerGrid.Count][];
        for (int i = 0; i < grpcGame.PlayerGrid.Count; i++)
        {
            game.PlayerGrid[i] = grpcGame.PlayerGrid[i].ToCharArray();
        }

        // Map Opponent Grid
        game.OpponentGrid = new bool?[grpcGame.OpponentGrid.Count][];
        for (int i = 0; i < grpcGame.OpponentGrid.Count; i++)
        {
            var row = grpcGame.OpponentGrid[i];
            game.OpponentGrid[i] = new bool?[row.Values.Count];
            for (int j = 0; j < row.Values.Count; j++)
            {
                // 0 = null, 1 = hit (true), 2 = miss (false)
                int val = row.Values[j];
                if (val == 0) game.OpponentGrid[i][j] = null;
                else if (val == 1) game.OpponentGrid[i][j] = true;
                else if (val == 2) game.OpponentGrid[i][j] = false;
            }
        }

        // Map History
        foreach (var h in grpcGame.History)
        {
            game.History.Add(new MoveHistory
            {
                Turn = h.Turn,
                PlayerMove = h.PlayerMove,
                PlayerResult = h.PlayerResult,
                AiMove = h.AiMove,
                AiResult = h.AiResult
            });
        }

        // Map Ships
        game.Ships = new List<ShipInfo>();
        foreach (var s in grpcGame.Ships)
        {
            game.Ships.Add(new ShipInfo
            {
                Letter = s.Letter[0],
                Size = s.Size,
                Row = s.Row,
                Col = s.Col,
                IsHorizontal = s.IsHorizontal
            });
        }

        return game;
    }
}
