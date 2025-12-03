namespace BattleShip.Models;

/// <summary>
/// Represents the current status of a Battleship game.
/// </summary>
public class GameStatus
{
    /// <summary>
    /// Unique identifier for the game.
    /// </summary>
    public string? GameId { get; set; }

    /// <summary>
    /// The current state of the game (Setup, Playing, GameOver).
    /// </summary>
    public GameState State { get; set; }

    /// <summary>
    /// The player's grid.
    /// Represents the placement of ships and opponent's hits.
    /// Typically characters like 'S' for Ship, 'X' for Hit, 'M' for Miss, etc.
    /// </summary>
    public char[][] PlayerGrid { get; set; } = [];

    public List<ShipInfo> Ships { get; set; } = new();

    /// <summary>
    // The opponent's grid from the player's perspective.
    /// null = not fired upon
    /// true = hit
    /// false = miss (water)
    /// </summary>
    public bool?[][] OpponentGrid { get; set; } = [];

    /// <summary>
    /// Indicates the winner of the game, if any.
    /// Null if the game is ongoing.
    /// </summary>
    public string? Winner { get; set; }

    /// <summary>
    /// Indicates if the game is over.
    /// </summary>
    public bool IsGameOver { get; set; }
    
    /// <summary>
    /// For multiplayer, indicates if it is the current player's turn.
    /// </summary>
    public bool IsMyTurn { get; set; }

    /// <summary>
    /// For multiplayer, indicates if the current player has placed their ships.
    /// </summary>
    public bool ShipsPlaced { get; set; }

    /// <summary>
    /// Result of the last attack by the player (e.g., "Hit", "Miss", "Sunk").
    /// </summary>
    public string? LastAttackResult { get; set; }

    /// <summary>
    /// Result of the last attack by the AI (e.g., "AI attacked B5: Miss").
    /// </summary>
    public string? LastAiAttackResult { get; set; }

    /// <summary>
    /// History of moves played in the game.
    /// </summary>
    public List<MoveHistory> History { get; set; } = new();
}
