using BattleShip.Models;

namespace BattleShip.API.Services;

public class AiService : IAiService
{
    public Queue<(int, int)> GenerateAiMoves(int gridSize)
    {
        var aiMoves = new Queue<(int, int)>();
        var evenMoves = new List<(int, int)>();
        var oddMoves = new List<(int, int)>();

        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                if ((r + c) % 2 == 0)
                {
                    evenMoves.Add((r, c));
                }
                else
                {
                    oddMoves.Add((r, c));
                }
            }
        }

        // Shuffle both lists
        var shuffledEven = evenMoves.OrderBy(_ => Random.Shared.Next()).ToList();
        var shuffledOdd = oddMoves.OrderBy(_ => Random.Shared.Next()).ToList();

        // Enqueue even moves first (Hunt efficiently)
        foreach (var move in shuffledEven)
        {
            aiMoves.Enqueue(move);
        }
        // Enqueue odd moves last (Cleanup for size 1 ships)
        foreach (var move in shuffledOdd)
        {
            aiMoves.Enqueue(move);
        }

        return aiMoves;
    }

    public (string? Move, string? Result) PerformAiTurn(InternalGame game)
    {
        int aiRow = -1, aiCol = -1;
        bool foundMove = false;
        int gridSize = game.PlayerGrid.Length; // Detect grid size dynamically

        // 1. Priority: Check Target Stack (Hunt Mode) - Only for Medium and Hard
        if (game.Difficulty != DifficultyLevel.Easy)
        {
            while (game.TargetStack.Count > 0)
            {
                var (r, c) = game.TargetStack.Pop();
                if (IsValidAttack(game.PlayerGrid, r, c))
                {
                    aiRow = r;
                    aiCol = c;
                    foundMove = true;
                    break;
                }
            }
        }

        // 2. Fallback: Search Mode
        if (!foundMove)
        {
            if (game.Difficulty == DifficultyLevel.Hard)
            {
                // Hard: Heatmap Strategy with Parity Optimization (Smarter)
                var bestMove = GetBestHeatmapMove(game, useParity: true);
                if (bestMove.HasValue)
                {
                    aiRow = bestMove.Value.Row;
                    aiCol = bestMove.Value.Col;
                    foundMove = true;
                }
            }
            else if (game.Difficulty == DifficultyLevel.Medium)
            {
                // Medium: Heatmap Strategy without Parity
                var bestMove = GetBestHeatmapMove(game, useParity: false);
                if (bestMove.HasValue)
                {
                    aiRow = bestMove.Value.Row;
                    aiCol = bestMove.Value.Col;
                    foundMove = true;
                }
            }
            
            // Easy (or fallback if queues empty): Random Random
            if (!foundMove)
            {
                // Simple random search
                int attempts = 0;
                while (!foundMove && attempts < 1000)
                {
                    attempts++;
                    int r = Random.Shared.Next(gridSize);
                    int c = Random.Shared.Next(gridSize);
                    if (IsValidAttack(game.PlayerGrid, r, c))
                    {
                        aiRow = r;
                        aiCol = c;
                        foundMove = true;
                    }
                }
                
                // Last resort: linear scan
                if (!foundMove)
                {
                    for (int r = 0; r < gridSize; r++)
                    {
                        for (int c = 0; c < gridSize; c++)
                        {
                            if (IsValidAttack(game.PlayerGrid, r, c))
                            {
                                aiRow = r;
                                aiCol = c;
                                foundMove = true;
                                goto MoveFound;
                            }
                        }
                    }
                }
            }
        }

        MoveFound:

        if (!foundMove)
        {
            return (null, null); // Should not happen unless board is full
        }

        string moveString = GetCoordinateString(aiRow, aiCol);
        char target = game.PlayerGrid[aiRow][aiCol];
        string aiResult;

        if (target != '\0' && target != 'X' && target != 'O')
        {
            // It's a Hit!
            char shipLetter = target;
            game.PlayerGrid[aiRow][aiCol] = 'X'; // Hit
            aiResult = "Hit!";
            
            // Update Intelligence
            if (game.Difficulty != DifficultyLevel.Easy)
            {
                game.CurrentShipHits.Add((aiRow, aiCol, shipLetter));
            }
            
            // Check if Sunk
            if (IsShipSunk(game.PlayerGrid, shipLetter))
            {
                aiResult = "Sunk!";
                
                // Remove from Alive Ships
                int size = GetShipSize(shipLetter);
                game.AlivePlayerShips.Remove(size);
                
                if (game.Difficulty != DifficultyLevel.Easy)
                {
                    // Remove hits belonging to the sunk ship
                    game.CurrentShipHits.RemoveAll(h => h.Item3 == shipLetter);

                    // Clear Target Stack (Reset to Search Mode or handle adjacent ships)
                    game.TargetStack.Clear();

                    // If we have remaining hits (from another ship), regenerate neighbors for them
                    if (game.CurrentShipHits.Count > 0)
                    {
                        foreach (var hit in game.CurrentShipHits)
                        {
                            AddSmartNeighbors(game, hit.Item1, hit.Item2);
                        }
                        // Re-apply filter if applicable
                        FilterTargetStack(game);
                    }
                }
            }
            else
            {
                // Not sunk, add neighbors to stack
                if (game.Difficulty != DifficultyLevel.Easy)
                {
                    AddSmartNeighbors(game, aiRow, aiCol);
                    FilterTargetStack(game);
                }
            }
        }
        else
        {
            // Miss
            if (game.PlayerGrid[aiRow][aiCol] == '\0')
            {
                game.PlayerGrid[aiRow][aiCol] = 'O'; // Miss
            }
            aiResult = "Miss";
        }

        return (moveString, aiResult);
    }

    private bool IsValidAttack(char[][] grid, int r, int c)
    {
        int gridSize = grid.Length;
        if (r < 0 || r >= gridSize || c < 0 || c >= gridSize) return false;
        char cell = grid[r][c];
        return cell != 'X' && cell != 'O';
    }

    private bool IsShipSunk(char[][] grid, char letter)
    {
        int gridSize = grid.Length;
        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                if (grid[r][c] == letter) return false;
            }
        }
        return true;
    }

    private int GetShipSize(char letter)
    {
        return letter switch
        {
            'A' => 4,
            'B' => 3,
            'C' => 3,
            'D' => 2,
            'E' => 2,
            'F' => 1,
            _ => 0
        };
    }

    private void AddSmartNeighbors(InternalGame game, int r, int c)
    {
        // If we have multiple hits, we can deduce orientation
        bool isHorizontal = false;
        bool isVertical = false;

        if (game.CurrentShipHits.Count > 1)
        {
            // Check alignment of hits
            var first = game.CurrentShipHits[0];
            var last = game.CurrentShipHits.Last(); // Or just check all
            
            if (first.Item1 == last.Item1) isHorizontal = true;
            if (first.Item2 == last.Item2) isVertical = true;
        }

        var potential = new List<(int, int)>();

        if (!isVertical) // If not strictly vertical, try horizontal neighbors
        {
            potential.Add((r, c - 1));
            potential.Add((r, c + 1));
        }
        if (!isHorizontal) // If not strictly horizontal, try vertical neighbors
        {
            potential.Add((r - 1, c));
            potential.Add((r + 1, c));
        }

        int minShipSize = game.AlivePlayerShips.Count > 0 ? game.AlivePlayerShips.Min() : 1;

        foreach (var (nr, nc) in potential)
        {
            if (IsValidAttack(game.PlayerGrid, nr, nc))
            {
                // Advanced Filter: Check if ship of minShipSize can fit
                // Determine orientation of the neighbor relative to the current hit (r, c)
                bool isNeighborHorizontal = (nr == r); 
                
                if (CanFitShip(game.PlayerGrid, nr, nc, minShipSize, isNeighborHorizontal))
                {
                    if (!game.TargetStack.Contains((nr, nc)))
                    {
                        game.TargetStack.Push((nr, nc));
                    }
                }
            }
        }
    }

    private bool CanFitShip(char[][] grid, int r, int c, int minSize, bool horizontal)
    {
        int gridSize = grid.Length;
        int count = 0;
        if (horizontal)
        {
            // Count left (including r,c)
            for (int col = c; col >= 0; col--)
            {
                if (grid[r][col] == 'O') break; // Miss blocks
                count++;
            }
            // Count right (excluding r,c)
            for (int col = c + 1; col < gridSize; col++)
            {
                if (grid[r][col] == 'O') break;
                count++;
            }
        }
        else // Vertical
        {
            // Count up (including r,c)
            for (int row = r; row >= 0; row--)
            {
                if (grid[row][c] == 'O') break;
                count++;
            }
            // Count down (excluding r,c)
            for (int row = r + 1; row < gridSize; row++)
            {
                if (grid[row][c] == 'O') break;
                count++;
            }
        }
        return count >= minSize;
    }

    private void FilterTargetStack(InternalGame game)
    {
        if (game.CurrentShipHits.Count < 2) return;

        // Determine orientation based on hits
        bool isHorizontal = game.CurrentShipHits.All(h => h.Item1 == game.CurrentShipHits[0].Item1);
        bool isVertical = game.CurrentShipHits.All(h => h.Item2 == game.CurrentShipHits[0].Item2);

        if (isHorizontal)
        {
            // Keep only targets on the same row
            var validTargets = game.TargetStack.Where(t => t.Item1 == game.CurrentShipHits[0].Item1).ToList();
            
            game.TargetStack.Clear();
            validTargets.Reverse(); // Reverse to maintain stack order (LIFO)
            foreach (var t in validTargets) game.TargetStack.Push(t);
        }
        else if (isVertical)
        {
            // Keep only targets on the same column
            var validTargets = game.TargetStack.Where(t => t.Item2 == game.CurrentShipHits[0].Item2).ToList();
            
            game.TargetStack.Clear();
            validTargets.Reverse(); // Reverse to maintain stack order (LIFO)
            foreach (var t in validTargets) game.TargetStack.Push(t);
        }
    }

    private string GetCoordinateString(int row, int col)
    {
        return $"{(char)('A' + row)}{col + 1}";
    }

    private (int Row, int Col)? GetBestHeatmapMove(InternalGame game, bool useParity)
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
        // If smallest ship is 1, parity doesn't help (we must check every cell)
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
            // Pick random from best moves to avoid predictability
            var choice = bestMoves[Random.Shared.Next(bestMoves.Count)];
            return (choice.Item1, choice.Item2);
        }

        return null;
    }

    private bool CanPlaceShip(char[][] grid, int r, int c, int size, bool horizontal)
    {
        int gridSize = grid.Length;
        if (horizontal)
        {
            if (c + size > gridSize) return false;
            for (int k = 0; k < size; k++)
            {
                char cell = grid[r][c + k];
                // 'X' and 'O' are blockers. We assume we are looking for NEW ships.
                // If we hit a ship ('X'), we can't place another ship ON TOP of it.
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
}
