using BattleShip.Models;

namespace BattleShip.API.Services.Strategies;

public class HardAiStrategy : IAiStrategy
{
    public (int Row, int Col)? GetNextMove(InternalGame game)
    {
        // Hard: Heatmap Strategy with Parity Optimization
        return AiStrategyHelper.GetBestHeatmapMove(game, useParity: true);
    }
}
