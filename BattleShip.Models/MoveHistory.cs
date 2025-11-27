namespace BattleShip.Models;

public class MoveHistory
{
    public int Turn { get; set; }
    public string PlayerMove { get; set; } = string.Empty;
    public string PlayerResult { get; set; } = string.Empty;
    public string AiMove { get; set; } = string.Empty;
    public string AiResult { get; set; } = string.Empty;
}
