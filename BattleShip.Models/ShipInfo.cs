namespace BattleShip.Models;

public class ShipInfo
{
    public char Letter { get; set; }
    public int Size { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsHorizontal { get; set; }
    public int Hits { get; set; }
}
