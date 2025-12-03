namespace BattleShip.API.Services;

public interface IAiService
{
    Queue<(int, int)> GenerateAiMoves(int gridSize);
    (string? Move, string? Result) PerformAiTurn(InternalGame game);
}
