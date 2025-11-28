using BattleShip.Models;

namespace BattleShip.API.Services;

public interface IGameService
{
    GameStatus CreateGame(DifficultyLevel difficulty);
    GameStatus Attack(Guid gameId, int row, int col);
    GameStatus Undo(Guid gameId);
    GameStatus UndoToTurn(Guid gameId, int turn);
}
