using System.Net.Http.Json;
using BattleShip.Models;
using BattleShip.Protos;

namespace BattleShip.App.Services;

public class GameClient
{
    private readonly HttpClient _httpClient;
    private readonly Game.GameClient _grpcClient;

    public GameStatus? CurrentGame { get; private set; }
    public string? Message { get; private set; }

    public GameClient(HttpClient httpClient, Game.GameClient grpcClient)
    {
        _httpClient = httpClient;
        _grpcClient = grpcClient;
    }

    private bool _useGrpc;

    public async Task StartGameAsync(bool useGrpc = false)
    {
        _useGrpc = useGrpc;
        try
        {
            if (useGrpc)
            {
                var grpcGame = await _grpcClient.CreateGameAsync(new Empty());
                CurrentGame = MapFromGrpc(grpcGame);
                Message = "Game Started (via gRPC)! Good luck.";
            }
            else
            {
                var response = await _httpClient.PostAsync("/game", null);
                if (response.IsSuccessStatusCode)
                {
                    CurrentGame = await response.Content.ReadFromJsonAsync<GameStatus>();
                    Message = "Game Started (via REST)! Good luck.";
                }
                else
                {
                    Message = "Failed to start game.";
                }
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
        }
    }

    private GameStatus MapFromGrpc(GrpcGameStatus grpcGame)
    {
        var game = new GameStatus
        {
            GameId = Guid.Parse(grpcGame.GameId),
            Winner = string.IsNullOrEmpty(grpcGame.Winner) ? null : grpcGame.Winner,
            LastAttackResult = string.IsNullOrEmpty(grpcGame.LastAttackResult) ? null : grpcGame.LastAttackResult,
            LastAiAttackResult = string.IsNullOrEmpty(grpcGame.LastAiAttackResult) ? null : grpcGame.LastAiAttackResult
            // IsGameOver is calculated property in model, but we can set it if we had a setter or just rely on Winner.
            // But wait, IsGameOver is getter only based on Winner. So setting Winner is enough.
        };

        // Map Player Grid
        game.PlayerGrid = new char[grpcGame.PlayerGrid.Count][];
        for (int i = 0; i < grpcGame.PlayerGrid.Count; i++)
        {
            game.PlayerGrid[i] = grpcGame.PlayerGrid[i].ToCharArray();
        }

        // Map Opponent Grid
        game.OpponentGrid = new bool?[grpcGame.OpponentGrid.Count][];
        for (int i = 0; i < grpcGame.OpponentGrid.Count; i++)
        {
            var row = grpcGame.OpponentGrid[i];
            game.OpponentGrid[i] = new bool?[row.Values.Count];
            for (int j = 0; j < row.Values.Count; j++)
            {
                // 0 = null, 1 = hit (true), 2 = miss (false)
                int val = row.Values[j];
                if (val == 0) game.OpponentGrid[i][j] = null;
                else if (val == 1) game.OpponentGrid[i][j] = true;
                else if (val == 2) game.OpponentGrid[i][j] = false;
            }
        }

        return game;
    }

    public async Task AttackAsync(int row, int col)
    {
        if (CurrentGame == null) return;

        try
        {
            if (_useGrpc)
            {
                var request = new GrpcAttackRequest 
                { 
                    GameId = CurrentGame.GameId.ToString(), 
                    Row = row, 
                    Col = col 
                };
                var grpcGame = await _grpcClient.AttackAsync(request);
                CurrentGame = MapFromGrpc(grpcGame);
            }
            else
            {
                var request = new AttackRequest { Row = row, Col = col };
                var response = await _httpClient.PostAsJsonAsync($"/game/{CurrentGame.GameId}/attack", request);Hit

                
                if (response.IsSuccessStatusCode)
                {
                    CurrentGame = await response.Content.ReadFromJsonAsync<GameStatus>();
                }
                else
                {
                    Message = "Attack failed.";
                    return;
                }
            }

            if (CurrentGame?.IsGameOver == true)
            {
                if ( CurrentGame.Winner=="Player")
                {
                    Message = "Vous avez gagné!";
                }
                else
                {
                    Message = "Vous avez perdu!";
                }
                
            }
            else
            {
                Message = $"{CurrentGame?.LastAttackResult} | {CurrentGame?.LastAiAttackResult}";
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
        }
    }
}
