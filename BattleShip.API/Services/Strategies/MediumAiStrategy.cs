using BattleShip.Models;

namespace BattleShip.API.Services.Strategies;

public class MediumAiStrategy : IAiStrategy
{
    public (int Row, int Col)? GetNextMove(InternalGame game)
    {
        // Medium: Heatmap Strategy without Parity
        return AiStrategyHelper.GetBestHeatmapMove(game, useParity: false);
    }
}
