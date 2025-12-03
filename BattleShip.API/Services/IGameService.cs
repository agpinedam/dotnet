using BattleShip.Models;

namespace BattleShip.API.Services;

public interface IGameService
{
    GameStatus CreateGame(DifficultyLevel difficulty, int gridSize);
    GameStatus Attack(Guid gameId, int row, int col, int gridSize);
    GameStatus Undo(Guid gameId);
    GameStatus UndoToTurn(Guid gameId, int turn);
}
