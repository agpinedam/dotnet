using BattleShip.Models;

namespace BattleShip.API.Services.Strategies;

public interface IAiStrategy
{
    (int Row, int Col)? GetNextMove(InternalGame game);
}
