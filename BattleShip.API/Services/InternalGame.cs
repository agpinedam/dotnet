using BattleShip.Models;

namespace BattleShip.API.Services;

public class InternalGame
{
    public Guid Id { get; set; }
    public GameState State { get; set; } = GameState.Setup;
    public char[][] PlayerGrid { get; set; } = [];
    public char[][] AiGrid { get; set; } = [];
    public List<ShipInfo> PlayerShips { get; set; } = new();
    public bool?[][] OpponentGrid { get; set; } = [];
    public Queue<(int, int)> AiMoves { get; set; } = new();
    public string? Winner { get; set; }
    public string? LastAttackResult { get; set; }
    public string? LastAiAttackResult { get; set; }
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    
    public List<MoveHistory> History { get; set; } = new();
    public Stack<InternalGame> PreviousStates { get; set; } = new();

    // AI Intelligence
    public Stack<(int, int)> TargetStack { get; set; } = new();
    public List<int> AlivePlayerShips { get; set; } = new();
    public List<(int, int, char)> CurrentShipHits { get; set; } = new();

    public const char CellHit = 'X';
    public const char CellMiss = 'O';
    public const char CellWater = '\0';

    public InternalGame DeepCopy()
    {
        var copy = new InternalGame
        {
            Id = this.Id,
            State = this.State,
            Winner = this.Winner,
            LastAttackResult = this.LastAttackResult,
            LastAiAttackResult = this.LastAiAttackResult,
            Difficulty = this.Difficulty,
            History = new List<MoveHistory>(this.History),
            PlayerShips = new List<ShipInfo>(this.PlayerShips), // Shallow copy of list is fine as ShipInfo is immutable-ish
            AiMoves = new Queue<(int, int)>(this.AiMoves),
            TargetStack = new Stack<(int, int)>(this.TargetStack.Reverse()), // Reverse to preserve order when pushing
            AlivePlayerShips = new List<int>(this.AlivePlayerShips),
            CurrentShipHits = new List<(int, int, char)>(this.CurrentShipHits)
        };

        // Deep copy arrays
        copy.PlayerGrid = this.PlayerGrid.Select(r => (char[])r.Clone()).ToArray();
        copy.AiGrid = this.AiGrid.Select(r => (char[])r.Clone()).ToArray();
        copy.OpponentGrid = this.OpponentGrid.Select(r => (bool?[])r.Clone()).ToArray();

        return copy;
    }

    public string ProcessPlayerAttack(int row, int col)
    {
        int gridSize = PlayerGrid.Length;
        if (row < 0 || row >= gridSize || col < 0 || col >= gridSize)
        {
            return "Out of Bounds";
        }

        char cell = AiGrid[row][col];
        if (cell == CellHit || cell == CellMiss)
        {
            return "Repeated";
        }

        if (cell != CellWater)
        {
            // Hit
            AiGrid[row][col] = CellHit;
            OpponentGrid[row][col] = true;
            return "Hit";
        }
        else
        {
            // Miss
            AiGrid[row][col] = CellMiss;
            OpponentGrid[row][col] = false;
            return "Miss";
        }
    }

    public bool CheckWin(char[][] grid)
    {
        // Flatten the array and check for any remaining ship parts.
        // A ship part is any char that isn't Water, Hit, or Miss.
        return !grid.SelectMany(row => row)
                    .Any(cell => cell != CellWater && cell != CellHit && cell != CellMiss);
    }
}
