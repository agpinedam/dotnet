namespace BattleShip.Models;

public class CreateGameRequest
{
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public int GridSize { get; set; } = 10;
}
