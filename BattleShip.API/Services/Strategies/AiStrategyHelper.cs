using BattleShip.Models;

namespace BattleShip.API.Services.Strategies;

public static class AiStrategyHelper
{
    public static (int Row, int Col)? GetBestHeatmapMove(InternalGame game, bool useParity)
    {
        int gridSize = game.PlayerGrid.Length;
        int[][] heatmap = new int[gridSize][];
        for (int i = 0; i < gridSize; i++) heatmap[i] = new int[gridSize];

        // Calculate probability for each remaining ship
        foreach (var shipSize in game.AlivePlayerShips)
        {
            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    // Check Horizontal
                    if (CanPlaceShip(game.PlayerGrid, r, c, shipSize, true))
                    {
                        for (int k = 0; k < shipSize; k++) heatmap[r][c + k]++;
                    }
                    // Check Vertical
                    if (CanPlaceShip(game.PlayerGrid, r, c, shipSize, false))
                    {
                        for (int k = 0; k < shipSize; k++) heatmap[r + k][c]++;
                    }
                }
            }
        }

        // Apply Parity Mask if requested and smallest ship > 1
        if (useParity && game.AlivePlayerShips.Count > 0 && game.AlivePlayerShips.Min() > 1)
        {
            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    if ((r + c) % 2 != 0)
                    {
                        heatmap[r][c] = 0; // Zero out odd cells
                    }
                }
            }
        }

        // Find cell with max score that hasn't been hit
        int maxScore = -1;
        var bestMoves = new List<(int, int)>();

        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                if (IsValidAttack(game.PlayerGrid, r, c))
                {
                    if (heatmap[r][c] > maxScore)
                    {
                        maxScore = heatmap[r][c];
                        bestMoves.Clear();
                        bestMoves.Add((r, c));
                    }
                    else if (heatmap[r][c] == maxScore)
                    {
                        bestMoves.Add((r, c));
                    }
                }
            }
        }

        if (bestMoves.Count > 0)
        {
            var choice = bestMoves[Random.Shared.Next(bestMoves.Count)];
            return (choice.Item1, choice.Item2);
        }

        // Fallback to random if heatmap fails (shouldn't happen unless board full)
        return new EasyAiStrategy().GetNextMove(game);
    }

    private static bool CanPlaceShip(char[][] grid, int r, int c, int size, bool horizontal)
    {
        int gridSize = grid.Length;
        if (horizontal)
        {
            if (c + size > gridSize) return false;
            for (int k = 0; k < size; k++)
            {
                char cell = grid[r][c + k];
                if (cell == 'X' || cell == 'O') return false;
            }
        }
        else
        {
            if (r + size > gridSize) return false;
            for (int k = 0; k < size; k++)
            {
                char cell = grid[r + k][c];
                if (cell == 'X' || cell == 'O') return false;
            }
        }
        return true;
    }

    private static bool IsValidAttack(char[][] grid, int r, int c)
    {
        int gridSize = grid.Length;
        if (r < 0 || r >= gridSize || c < 0 || c >= gridSize) return false;
        char cell = grid[r][c];
        return cell != 'X' && cell != 'O';
    }
}
