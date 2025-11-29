namespace BattleShip.Models;

public class AttackRequest
{
    public int Row { get; set; }
    public int Col { get; set; }
    public int GridSize { get; set; } = 10;
}
