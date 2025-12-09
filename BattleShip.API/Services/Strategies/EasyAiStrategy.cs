using BattleShip.Models;

namespace BattleShip.API.Services.Strategies;

public class EasyAiStrategy : IAiStrategy
{
    public (int Row, int Col)? GetNextMove(InternalGame game)
    {
        // Easy: Just random moves, or linear scan if random fails
        // The AiService will handle the "Hunt" mode (TargetStack) before calling this strategy
        // or we can decide that Easy mode DOES NOT use Hunt mode.
        // Based on original code: Easy mode does NOT use TargetStack logic in the same way (it was skipped in PerformAiTurn).
        
        // Original code for Easy:
        // 1. Random attempts
        // 2. Linear scan
        
        int gridSize = game.PlayerGrid.Length;
        int attempts = 0;
        
        while (attempts < 100)
        {
            attempts++;
            int r = Random.Shared.Next(gridSize);
            int c = Random.Shared.Next(gridSize);
            if (IsValidAttack(game.PlayerGrid, r, c))
            {
                return (r, c);
            }
        }
        
        // Fallback: Linear scan
        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                if (IsValidAttack(game.PlayerGrid, r, c))
                {
                    return (r, c);
                }
            }
        }
        
        return null;
    }

    private bool IsValidAttack(char[][] grid, int r, int c)
    {
        int gridSize = grid.Length;
        if (r < 0 || r >= gridSize || c < 0 || c >= gridSize) return false;
        char cell = grid[r][c];
        return cell != 'X' && cell != 'O';
    }
}
