using BattleShip.Models;

namespace BattleShip.API.Services;

public class InternalGame
{
    public Guid Id { get; set; }
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

    public InternalGame DeepCopy()
    {
        var copy = new InternalGame
        {
            Id = this.Id,
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
}
