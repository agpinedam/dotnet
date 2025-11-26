using System.Net.Http.Json;
using BattleShip.Models;

namespace BattleShip.App.Services;

public class GameClient
{
    private readonly HttpClient _httpClient;

    public GameStatus? CurrentGame { get; private set; }
    public string? Message { get; private set; }

    public GameClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task StartGameAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/game", null);
            if (response.IsSuccessStatusCode)
            {
                CurrentGame = await response.Content.ReadFromJsonAsync<GameStatus>();
                Message = "Game Started! Good luck.";
            }
            else
            {
                Message = "Failed to start game.";
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
        }
    }

    public async Task AttackAsync(int row, int col)
    {
        if (CurrentGame == null) return;

        try
        {
            var request = new AttackRequest { Row = row, Col = col };
            var response = await _httpClient.PostAsJsonAsync($"/game/{CurrentGame.GameId}/attack", request);
            
            if (response.IsSuccessStatusCode)
            {
                CurrentGame = await response.Content.ReadFromJsonAsync<GameStatus>();
                
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
            else
            {
                Message = "Attack failed.";
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
        }
    }
}
