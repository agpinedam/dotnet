using System.Collections.Concurrent;
using BattleShip.API.Services;
using BattleShip.Models;
using Microsoft.Extensions.Configuration;

namespace BattleShip.API.Services;

public class GameService : IGameService
{
    private readonly IAiService _aiService;
    private readonly List<(char Letter, int Size)> _shipConfig;
    
    private const char CellHit = 'X';
    private const char CellMiss = 'O';
    private const char CellWater = '\0'; 

    // Thread-safe storage for game states. 
    private static readonly ConcurrentDictionary<Guid, InternalGame> Games = new();

    public GameService(IAiService aiService, IConfiguration configuration)
    {
        _aiService = aiService;
        
        var configItems = configuration.GetSection("ShipConfig").Get<List<ShipConfigItem>>();
        if (configItems != null)
        {
            _shipConfig = configItems.Select(x => (x.Letter[0], x.Size)).ToList();
        }
        else
        {
            // Fallback default
            _shipConfig = new List<(char, int)>
            {
                ('A', 4), ('B', 3), ('C', 3), ('D', 2), ('E', 2), ('F', 1)
            };
        }
    }

    private class ShipConfigItem
    {
        public string Letter { get; set; } = string.Empty;
        public int Size { get; set; }
    }

    public GameStatus CreateGame(DifficultyLevel difficulty, int gridSize)
    {
        var gameId = Guid.NewGuid();

        // 1. Generate Grids
        var (playerGrid, playerShips) = GenerateRandomGrid(gridSize);
        var (aiGrid, _) = GenerateRandomGrid(gridSize);

        // 2. Prepare AI Strategy
        var aiMoves = _aiService.GenerateAiMoves(gridSize);

        // 3. Initialize Game State
        var game = new InternalGame
        {
            Id = gameId,
            State = GameState.Setup,
            Difficulty = difficulty,
            PlayerGrid = playerGrid,
            PlayerShips = playerShips,
            AiGrid = aiGrid,
            AiMoves = aiMoves,
            OpponentGrid = InitFogOfWarGrid(gridSize),
            // Track alive ships for game logic optimization
            AlivePlayerShips = _shipConfig.Select(s => s.Size).ToList() 
        };


        // Thread-safe addition
        Games.TryAdd(gameId, game);

        return MapToStatus(game);
    }

    public GameStatus PlaceShips(Guid gameId, List<ShipInfo> ships)
    {
        var game = GetGameOrThrow(gameId);

        // Use a lock to prevent race conditions if user double-clicks confirm
        lock (game)
        {
            int gridSize = game.PlayerGrid.Length;
            var newGrid = CreateEmptyGrid<char>(gridSize);

            // Validation and Placement Phase
            foreach (var ship in ships)
            {
                ValidateShipBounds(ship, gridSize);

                if (!CanPlaceShip(newGrid, ship.Row, ship.Col, ship.Size, ship.IsHorizontal))
                {
                    throw new ArgumentException($"Invalid placement for ship {ship.Letter}. It overlaps with another ship.");
                }

                PlaceShipOnGrid(newGrid, ship.Row, ship.Col, ship.Size, ship.IsHorizontal, ship.Letter);
            }

            // Commit State
            game.PlayerGrid = newGrid;
            game.PlayerShips = ships;
            game.State = GameState.Playing;

            return MapToStatus(game);
        }
    }

    public GameStatus Attack(Guid gameId, int row, int col)
    {
        var game = GetGameOrThrow(gameId);

        lock (game)
        {
            if (game.Winner != null) return MapToStatus(game);

            // 1. Save State for Undo 
            game.PreviousStates.Push(game.DeepCopy());

            int gridSize = game.PlayerGrid.Length;
            var currentTurn = new MoveHistory
            {
                Turn = game.History.Count + 1,
                PlayerMove = GetCoordinateString(row, col)
            };

            // 2. Player Attack Logic
            string attackResult = game.ProcessPlayerAttack(row, col);
            currentTurn.PlayerResult = attackResult;

            // Check Player Win
            if (game.CheckWin(game.AiGrid))
            {
                game.Winner = "Player";
                game.State = GameState.GameOver;
                game.History.Add(currentTurn);
                return MapToStatus(game);
            }

            // 3. AI Attack Logic
            var aiTurnResult = _aiService.PerformAiTurn(game);
            currentTurn.AiMove = aiTurnResult.Move ?? "";
            currentTurn.AiResult = aiTurnResult.Result ?? "";

            game.History.Add(currentTurn);

            // Check AI Win
            if (game.CheckWin(game.PlayerGrid))
            {
                game.Winner = "AI";
                game.State = GameState.GameOver;
            }

            return MapToStatus(game);
        }
    }

    public GameStatus Undo(Guid gameId)
    {
        var game = GetGameOrThrow(gameId);

        lock (game)
        {
            if (game.PreviousStates.Count > 0)
            {
                var previousState = game.PreviousStates.Pop();
                
                previousState.PreviousStates = game.PreviousStates; 
                
                // Update the thread-safe dictionary
                Games[gameId] = previousState;
                
                return MapToStatus(previousState);
            }
            return MapToStatus(game);
        }
    }

    public GameStatus UndoToTurn(Guid gameId, int targetTurn)
    {
        var game = GetGameOrThrow(gameId);

        lock (game)
        {
            int currentTurnIndex = game.History.Count;
            int stepsToUndo = currentTurnIndex - targetTurn;

            if (stepsToUndo <= 0) return MapToStatus(game);

            InternalGame restoredGame = game;

            // Pop N times from the stack
            for (int i = 0; i < stepsToUndo; i++)
            {
                if (restoredGame.PreviousStates.Count == 0) break;

                var stack = restoredGame.PreviousStates;
                restoredGame = stack.Pop();
                restoredGame.PreviousStates = stack;
            }

            Games[gameId] = restoredGame;
            return MapToStatus(restoredGame);
        }
    }

    private InternalGame GetGameOrThrow(Guid gameId)
    {
        if (!Games.TryGetValue(gameId, out var game))
        {
            throw new ArgumentException("Game not found");
        }
        return game;
    }

    private (string ResultDescription, bool IsHit) ProcessPlayerShot(InternalGame game, int row, int col)
    {
        int gridSize = game.AiGrid.Length;

        // Validation
        if (row < 0 || row >= gridSize || col < 0 || col >= gridSize)
        {
            return ("Out of bounds", false);
        }

        char currentCell = game.AiGrid[row][col];

        // Check if already fired
        if (currentCell == CellHit || currentCell == CellMiss)
        {
            return ("Already fired here!", false);
        }

        // Logic: Hit or Miss
        if (currentCell != CellWater) 
        {
            // It is a ship
            game.AiGrid[row][col] = CellHit;
            game.OpponentGrid[row][col] = true; // True = Hit
            return ("Hit!", true);
        }
        else
        {
            // It is water
            game.AiGrid[row][col] = CellMiss;
            game.OpponentGrid[row][col] = false; // False = Miss
            return ("Miss", false);
        }
    }

    private GameStatus MapToStatus(InternalGame game)
    {
        return new GameStatus
        {
            GameId = game.Id.ToString(),
            State = game.State,
            PlayerGrid = game.PlayerGrid,
            Ships = game.PlayerShips,
            OpponentGrid = game.OpponentGrid,
            Winner = game.Winner,
            IsGameOver = game.State == GameState.GameOver,
            LastAttackResult = game.LastAttackResult,
            LastAiAttackResult = game.LastAiAttackResult,
            History = game.History
        };
    }

    // -- GRID GENERATION & MANIPULATION --

    private (char[][] Grid, List<ShipInfo> Ships) GenerateRandomGrid(int gridSize)
    {
        var grid = CreateEmptyGrid<char>(gridSize);
        var placedShips = new List<ShipInfo>();

        foreach (var (letter, size) in _shipConfig)
        {
            var info = TryPlaceRandomShip(grid, letter, size, gridSize);
            if (info != null)
            {
                placedShips.Add(info);
            }
        }

        return (grid, placedShips);
    }

    private ShipInfo? TryPlaceRandomShip(char[][] grid, char letter, int size, int gridSize)
    {
        const int MaxAttempts = 100;
        
        for (int i = 0; i < MaxAttempts; i++)
        {
            bool horizontal = Random.Shared.Next(2) == 0;
            
            // Constrain random range to ensure it fits within bounds
            int maxRow = horizontal ? gridSize : gridSize - size;
            int maxCol = horizontal ? gridSize - size : gridSize;

            int row = Random.Shared.Next(maxRow);
            int col = Random.Shared.Next(maxCol);

            if (CanPlaceShip(grid, row, col, size, horizontal))
            {
                PlaceShipOnGrid(grid, row, col, size, horizontal, letter);
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

    private void ValidateShipBounds(ShipInfo ship, int gridSize)
    {
        int endRow = ship.IsHorizontal ? ship.Row : ship.Row + ship.Size - 1;
        int endCol = ship.IsHorizontal ? ship.Col + ship.Size - 1 : ship.Col;

        if (ship.Row < 0 || ship.Col < 0 || endRow >= gridSize || endCol >= gridSize)
        {
             throw new ArgumentException($"Ship {ship.Letter} is out of bounds.");
        }
    }

    private bool CanPlaceShip(char[][] grid, int row, int col, int size, bool horizontal)
    {
        // NOTE: We assume bounds are already checked by ValidateShipBounds or Random logic
        if (horizontal)
        {
            for (int c = col; c < col + size; c++)
                if (grid[row][c] != CellWater) return false;
        }
        else
        {
            for (int r = row; r < row + size; r++)
                if (grid[r][col] != CellWater) return false;
        }
        return true;
    }

    private void PlaceShipOnGrid(char[][] grid, int row, int col, int size, bool horizontal, char letter)
    {
        if (horizontal)
        {
            for (int c = col; c < col + size; c++) grid[row][c] = letter;
        }
        else
        {
            for (int r = row; r < row + size; r++) grid[r][col] = letter;
        }
    }

    private T[][] CreateEmptyGrid<T>(int size)
    {
        var grid = new T[size][];
        for (int i = 0; i < size; i++)
        {
            grid[i] = new T[size];
        }
        return grid;
    }

    private bool?[][] InitFogOfWarGrid(int gridSize)
    {
        return CreateEmptyGrid<bool?>(gridSize);
    }

    private string GetCoordinateString(int row, int col)
    {
        // Convert 0,0 to A1
        return $"{(char)('A' + row)}{col + 1}";
    }
}
