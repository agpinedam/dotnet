using BattleShip.Models;

namespace BattleShip.API.Services;

public class GameService : IGameService
{
    // In-memory storage for the game states (including the secret AI grid)
    private static readonly Dictionary<Guid, InternalGame> _games = new();

    public GameStatus CreateGame()
    {
        var gameId = Guid.NewGuid();

        // Generate Player Grid
        var playerGrid = GenerateGrid();

        // Generate AI Grid (Secret)
        var aiGrid = GenerateGrid();

        // Generate AI Moves Queue (Shuffled)
        var aiMoves = new Queue<(int, int)>();
        var allMoves = new List<(int, int)>();
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 10; c++)
            {
                allMoves.Add((r, c));
            }
        }
        // Shuffle
        var shuffledMoves = allMoves.OrderBy(_ => Random.Shared.Next()).ToList();
        foreach (var move in shuffledMoves)
        {
            aiMoves.Enqueue(move);
        }

        var game = new InternalGame
        {
            Id = gameId,
            PlayerGrid = playerGrid,
            AiGrid = aiGrid,
            AiMoves = aiMoves,
            OpponentGrid = InitEmptyBoolGrid()
        };

        _games[gameId] = game;

        return new GameStatus
        {
            GameId = gameId,
            PlayerGrid = playerGrid,
            OpponentGrid = game.OpponentGrid
        };
    }

    public GameStatus Attack(Guid gameId, int row, int col)
    {
        if (!_games.TryGetValue(gameId, out var game))
        {
            throw new ArgumentException("Game not found");
        }

        if (game.Winner != null)
        {
            return GetGameStatus(game);
        }

        // Save state for Undo
        game.PreviousStates.Push(game.DeepCopy());

        var currentTurn = new MoveHistory
        {
            Turn = game.History.Count + 1,
            PlayerMove = GetCoordinateString(row, col)
        };

        // Player Attack
        string attackResult = "Miss";
        if (row >= 0 && row < 10 && col >= 0 && col < 10)
        {
            char target = game.AiGrid[row][col];
            
            if (target == 'X' || target == 'O')
            {
                attackResult = "Already fired here!";
            }
            else if (target != '\0') // It's a ship (A-F)
            {
                game.AiGrid[row][col] = 'X'; // Mark as Hit on internal grid
                game.OpponentGrid[row][col] = true; // Update player's view
                attackResult = "Hit!";
            }
            else // It's water
            {
                game.AiGrid[row][col] = 'O'; // Mark as Miss on internal grid
                game.OpponentGrid[row][col] = false; // Update player's view
                attackResult = "Miss";
            }
        }
        else
        {
            attackResult = "Out of bounds";
        }

        game.LastAttackResult = attackResult;
        currentTurn.PlayerResult = attackResult;

        // Check Player Win
        if (CheckWin(game.AiGrid))
        {
            game.Winner = "Player";
            game.History.Add(currentTurn);
            return GetGameStatus(game);
        }

        // AI Turn
        if (game.AiMoves.Count > 0)
        {
            var (aiRow, aiCol) = game.AiMoves.Dequeue();
            currentTurn.AiMove = GetCoordinateString(aiRow, aiCol);
            
            char target = game.PlayerGrid[aiRow][aiCol];
            string aiResult;
            
            if (target != '\0' && target != 'X' && target != 'O')
            {
                game.PlayerGrid[aiRow][aiCol] = 'X'; // Hit
                aiResult = "Hit!";
                game.LastAiAttackResult = $"AI attacked {GetCoordinateString(aiRow, aiCol)}: Hit!";
            }
            else
            {
                // Only mark as miss if it wasn't already hit/miss (though AI shouldn't repeat moves with current logic)
                if (game.PlayerGrid[aiRow][aiCol] == '\0')
                {
                    game.PlayerGrid[aiRow][aiCol] = 'O'; // Miss
                }
                aiResult = "Miss";
                game.LastAiAttackResult = $"AI attacked {GetCoordinateString(aiRow, aiCol)}: Miss";
            }
            currentTurn.AiResult = aiResult;

            // Check AI Win
            if (CheckWin(game.PlayerGrid))
            {
                game.Winner = "AI";
            }
        }
        
        game.History.Add(currentTurn);

        return GetGameStatus(game);
    }

    public GameStatus Undo(Guid gameId)
    {
        if (!_games.TryGetValue(gameId, out var game))
        {
            throw new ArgumentException("Game not found");
        }

        if (game.PreviousStates.Count > 0)
        {
            var previousState = game.PreviousStates.Pop();
            
            previousState.PreviousStates = game.PreviousStates;
            
            // 4. Update the dictionary
            _games[gameId] = previousState;
            
            return GetGameStatus(previousState);
        }

        return GetGameStatus(game);
    }

    private bool CheckWin(char[][] grid)
    {
        // Check if any ship parts (A-F) remain.
        // If we find any character that is NOT '\0', 'X', or 'O', it means a ship is still alive.
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 10; c++)
            {
                char cell = grid[r][c];
                if (cell != '\0' && cell != 'X' && cell != 'O')
                {
                    return false; // Found a ship part
                }
            }
        }
        return true; // No ship parts found
    }

    private GameStatus GetGameStatus(InternalGame game)
    {
        return new GameStatus
        {
            GameId = game.Id,
            PlayerGrid = game.PlayerGrid,
            OpponentGrid = game.OpponentGrid,
            Winner = game.Winner,
            LastAttackResult = game.LastAttackResult,
            LastAiAttackResult = game.LastAiAttackResult,
            History = game.History
        };
    }

    private bool?[][] InitEmptyBoolGrid()
    {
        var grid = new bool?[10][];
        for (int i = 0; i < 10; i++)
        {
            grid[i] = new bool?[10];
        }
        return grid;
    }

    private string GetCoordinateString(int row, int col)
    {
        return $"{(char)('A' + row)}{col + 1}";
    }

    private char[][] GenerateGrid()
    {
        // Initialize jagged array
        var grid = new char[10][];
        for (int i = 0; i < 10; i++)
        {
            grid[i] = new char[10];
            // Array is initialized to '\0' by default in C#
        }

        // Define ships: Letter and Size
        // Requirement: Ships A-F, Sizes 1-4
        var ships = new (char Letter, int Size)[]
        {
            ('A', 4),
            ('B', 3),
            ('C', 3),
            ('D', 2),
            ('E', 2),
            ('F', 1)
        };

        foreach (var ship in ships)
        {
            PlaceShip(grid, ship.Letter, ship.Size);
        }

        return grid;
    }

    private void PlaceShip(char[][] grid, char letter, int size)
    {
        bool placed = false;
        int attempts = 0;
        while (!placed && attempts < 100) // Safety break
        {
            attempts++;
            // Orientation: 0 = Horizontal, 1 = Vertical
            bool horizontal = Random.Shared.Next(2) == 0;

            // Grid dimensions
            int rows = 10;
            int cols = 10;

            int row, col;

            if (horizontal)
            {
                // Ensure it fits horizontally
                // col must be between 0 and 10 - size
                // Random.Shared.Next(max) returns 0 to max-1
                // We want 0 to (10-size). So max should be (10-size) + 1
                col = Random.Shared.Next(cols - size + 1);
                row = Random.Shared.Next(rows);
            }
            else
            {
                // Ensure it fits vertically
                col = Random.Shared.Next(cols);
                row = Random.Shared.Next(rows - size + 1);
            }

            if (CanPlace(grid, row, col, size, horizontal))
            {
                DoPlace(grid, row, col, size, horizontal, letter);
                placed = true;
            }
        }
    }

    private bool CanPlace(char[][] grid, int row, int col, int size, bool horizontal)
    {
        if (horizontal)
        {
            for (int c = col; c < col + size; c++)
            {
                if (grid[row][c] != '\0') return false;
            }
        }
        else
        {
            for (int r = row; r < row + size; r++)
            {
                if (grid[r][col] != '\0') return false;
            }
        }
        return true;
    }

    private void DoPlace(char[][] grid, int row, int col, int size, bool horizontal, char letter)
    {
        if (horizontal)
        {
            for (int c = col; c < col + size; c++)
            {
                grid[row][c] = letter;
            }
        }
        else
        {
            for (int r = row; r < row + size; r++)
            {
                grid[r][col] = letter;
            }
        }
    }

    // Internal class to hold the full game state
    private class InternalGame
    {
        public Guid Id { get; set; }
        public char[][] PlayerGrid { get; set; } = [];
        public char[][] AiGrid { get; set; } = [];
        public bool?[][] OpponentGrid { get; set; } = [];
        public Queue<(int, int)> AiMoves { get; set; } = new();
        public string? Winner { get; set; }
        public string? LastAttackResult { get; set; }
        public string? LastAiAttackResult { get; set; }
        
        public List<MoveHistory> History { get; set; } = new();
        public Stack<InternalGame> PreviousStates { get; set; } = new();

        public InternalGame DeepCopy()
        {
            var copy = new InternalGame
            {
                Id = this.Id,
                Winner = this.Winner,
                LastAttackResult = this.LastAttackResult,
                LastAiAttackResult = this.LastAiAttackResult,
                History = new List<MoveHistory>(this.History),
                AiMoves = new Queue<(int, int)>(this.AiMoves),
            };

            // Deep copy arrays
            copy.PlayerGrid = this.PlayerGrid.Select(r => (char[])r.Clone()).ToArray();
            copy.AiGrid = this.AiGrid.Select(r => (char[])r.Clone()).ToArray();
            copy.OpponentGrid = this.OpponentGrid.Select(r => (bool?[])r.Clone()).ToArray();

            return copy;
        }
    }
}
