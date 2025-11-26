using BattleShip.Models;

namespace BattleShip.API.Services;

public interface IGameService
{
    GameStatus CreateGame();
    GameStatus Attack(Guid gameId, int row, int col);
}
