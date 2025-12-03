using BattleShip.Models;

namespace BattleShip.API.Services;

public interface IGameService
{
    GameStatus CreateGame(DifficultyLevel difficulty, int gridSize);
    GameStatus PlaceShips(Guid gameId, List<ShipInfo> ships);
    GameStatus Attack(Guid gameId, int row, int col);
    GameStatus Undo(Guid gameId);
    GameStatus UndoToTurn(Guid gameId, int turn);
}
