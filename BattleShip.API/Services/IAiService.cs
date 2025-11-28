namespace BattleShip.API.Services;

public interface IAiService
{
    Queue<(int, int)> GenerateAiMoves();
    (string? Move, string? Result) PerformAiTurn(InternalGame game);
}
