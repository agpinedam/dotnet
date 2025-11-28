using BattleShip.Models;

namespace BattleShip.API.Services;

public class GameService : IGameService
{
    private readonly IAiService _aiService;
    // In-memory storage for the game states (including the secret AI grid)
    private static readonly Dictionary<Guid, InternalGame> Games = new();

    public GameService(IAiService aiService)
    {
        _aiService = aiService;
    }

    public GameStatus CreateGame(DifficultyLevel difficulty, int gridSize)
    {
        var gameId = Guid.NewGuid();

        // Generate Player Grid
        var (playerGrid, playerShips) = GenerateGrid(gridSize);

        // Generate AI Grid (Secret)
        var (aiGrid, _) = GenerateGrid(gridSize);

        // Generate AI Moves Queue (Parity-based Strategy)
        var aiMoves = _aiService.GenerateAiMoves();

        var game = new InternalGame
        {
            Id = gameId,
            Difficulty = difficulty,
            PlayerGrid = playerGrid,
            PlayerShips = playerShips,
            AiGrid = aiGrid,
            AiMoves = aiMoves,
            OpponentGrid = InitEmptyBoolGrid(),
            AlivePlayerShips = new List<int> { 4, 3, 3, 2, 2, 1 }
        };

        Games[gameId] = game;

        return new GameStatus
        {
            GameId = gameId,
            PlayerGrid = playerGrid,
            Ships = playerShips,
            OpponentGrid = game.OpponentGrid
        };
    }

    public GameStatus Attack(Guid gameId, int row, int col)
    {
        if (!Games.TryGetValue(gameId, out var game))
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
        string attackResult;
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
        var (aiMove, aiResult) = _aiService.PerformAiTurn(game);
        
        if (aiMove != null)
        {
            currentTurn.AiMove = aiMove;
            currentTurn.AiResult = aiResult ?? string.Empty;
            game.LastAiAttackResult = $"AI attacked {aiMove}: {aiResult}";

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
        if (!Games.TryGetValue(gameId, out var game))
        {
            throw new ArgumentException("Game not found");
        }

        if (game.PreviousStates.Count > 0)
        {
            var previousState = game.PreviousStates.Pop();
            
            previousState.PreviousStates = game.PreviousStates;
            
            // 4. Update the dictionary
            Games[gameId] = previousState;
            
            return GetGameStatus(previousState);
        }

        return GetGameStatus(game);
    }

    public GameStatus UndoToTurn(Guid gameId, int turn)
    {
        if (!Games.TryGetValue(gameId, out var game))
        {
            throw new ArgumentException("Game not found");
        }

        int currentTurn = game.History.Count;
        int stepsToUndo = currentTurn - turn;

        if (stepsToUndo <= 0)
        {
            return GetGameStatus(game);
        }

        InternalGame targetState = game;
        
        for (int i = 0; i < stepsToUndo; i++)
        {
            if (targetState.PreviousStates.Count > 0)
            {
                var tempStack = targetState.PreviousStates; // The stack to continue popping from
                targetState = tempStack.Pop();
                targetState.PreviousStates = tempStack; // Restore the stack reference
            }
            else
            {
                break;
            }
        }

        Games[gameId] = targetState;
        return GetGameStatus(targetState);
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
            Ships = game.PlayerShips,
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

    private (char[][] Grid, List<ShipInfo> Ships) GenerateGrid(int gridSize)
    {
        // Initialize jagged array
        var grid = new char[gridSize][];
        for (int i = 0; i < gridSize; i++)
        {
            grid[i] = new char[gridSize];
            // Array is initialized to '\0' by default in C#
        }

        var placedShips = new List<ShipInfo>();

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
            var info = PlaceShip(grid, ship.Letter, ship.Size);
            if (info != null)
            {
                placedShips.Add(info);
            }
        }

        return (grid, placedShips);
    }

    private ShipInfo? PlaceShip(char[][] grid, char letter, int size)
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
                return new ShipInfo
                {
                    Letter = letter,
                    Size = size,
                    Row = row,
                    Col = col,
                    IsHorizontal = horizontal
                };
            }
        }
        return null;
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
}
