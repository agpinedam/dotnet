using System.Net.Http.Json;
using BattleShip.App.Constants;
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

    public void ResetGame()
    {
        CurrentGame = null;
        Message = null;
    }

    public async Task StartGameAsync(bool useGrpc = false, DifficultyLevel difficulty = DifficultyLevel.Medium, int gridSize = 10)
    {
        _useGrpc = useGrpc;
        try
        {
            if (useGrpc)
            {
                var request = new GrpcCreateGameRequest { Difficulty = (int)difficulty, GridSize = gridSize };
                var grpcGame = await _grpcClient.CreateGameAsync(request);
                CurrentGame = GrpcMapper.MapFromGrpc(grpcGame);
                Message = $"Game Started (via gRPC, {difficulty})! Good luck.";
            }
            else
            {
                var request = new CreateGameRequest { Difficulty = difficulty, GridSize = gridSize };
                var response = await _httpClient.PostAsJsonAsync(ApiConstants.BaseUrl, request);
                if (response.IsSuccessStatusCode)
                {
                    CurrentGame = await response.Content.ReadFromJsonAsync<GameStatus>();
                    Message = $"Game Started (via REST, {difficulty})! Good luck.";
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

    public async Task PlaceShipsAsync(List<ShipInfo> ships)
    {
        if (CurrentGame == null) return;

        try
        {
            if (_useGrpc)
            {
                var grpcRequest = new GrpcPlaceShipsRequest
                {
                    GameId = CurrentGame.GameId ?? string.Empty
                };

                foreach (var s in ships)
                {
                    grpcRequest.Ships.Add(new GrpcShipInfo
                    {
                        Letter = s.Letter.ToString(),
                        Size = s.Size,
                        Row = s.Row,
                        Col = s.Col,
                        IsHorizontal = s.IsHorizontal
                    });
                }

                var grpcGame = await _grpcClient.PlaceShipsAsync(grpcRequest);
                CurrentGame = GrpcMapper.MapFromGrpc(grpcGame);
                Message = "Ships placed! Battle starts (gRPC).";
            }
            else
            {
                if (CurrentGame.GameId == null) return;
                var request = new PlaceShipsRequest { GameId = Guid.Parse(CurrentGame.GameId), Ships = ships };
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstants.BaseUrl}/{CurrentGame.GameId}{ApiConstants.PlaceShipsEndpoint}", request);
                
                if (response.IsSuccessStatusCode)
                {
                    CurrentGame = await response.Content.ReadFromJsonAsync<GameStatus>();
                    Message = "Ships placed! Battle starts.";
                }
                else
                {
                    Message = "Failed to place ships. Check for overlaps.";
                }
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
            if (_useGrpc)
            {
                var request = new GrpcAttackRequest 
                { 
                    GameId = CurrentGame.GameId ?? string.Empty, 
                    Row = row, 
                    Col = col 
                };
                var grpcGame = await _grpcClient.AttackAsync(request);
                CurrentGame = GrpcMapper.MapFromGrpc(grpcGame);
            }
            else
            {
                var request = new AttackRequest { Row = row, Col = col };
                var response = await _httpClient.PostAsJsonAsync($"{ApiConstants.BaseUrl}/{CurrentGame.GameId}{ApiConstants.AttackEndpoint}", request);
                
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
                if (CurrentGame.Winner == "Player")
                {
                    Message = "You Won! Congratulations!";
                }
                else
                {
                    Message = "You Lost! Better luck next time.";
                }
            }
            else
            {
                // Display the result of the last turn (Player's move and AI's response)
                Message = $"{CurrentGame?.LastAttackResult} | {CurrentGame?.LastAiAttackResult}";
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
        }
    }

    public async Task UndoAsync()
    {
        if (CurrentGame == null) return;

        try
        {
            if (_useGrpc)
            {
                var request = new GrpcUndoRequest { GameId = CurrentGame.GameId ?? string.Empty };
                var grpcGame = await _grpcClient.UndoAsync(request);
                CurrentGame = GrpcMapper.MapFromGrpc(grpcGame);
            }
            else
            {
                var response = await _httpClient.PostAsync($"{ApiConstants.BaseUrl}/{CurrentGame.GameId}{ApiConstants.UndoEndpoint}", null);
                if (response.IsSuccessStatusCode)
                {
                    CurrentGame = await response.Content.ReadFromJsonAsync<GameStatus>();
                }
                else
                {
                    Message = "Undo failed.";
                    return;
                }
            }
            Message = "Undo successful.";
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
        }
    }

    public async Task UndoToTurnAsync(int turn)
    {
        if (CurrentGame == null) return;

        try
        {
            if (_useGrpc)
            {
                var request = new GrpcUndoToTurnRequest { GameId = CurrentGame.GameId ?? string.Empty, Turn = turn };
                var grpcGame = await _grpcClient.UndoToTurnAsync(request);
                CurrentGame = GrpcMapper.MapFromGrpc(grpcGame);
            }
            else
            {
                var response = await _httpClient.PostAsync($"{ApiConstants.BaseUrl}/{CurrentGame.GameId}{ApiConstants.UndoToTurnEndpoint}/{turn}", null);
                if (response.IsSuccessStatusCode)
                {
                    CurrentGame = await response.Content.ReadFromJsonAsync<GameStatus>();
                    Message = $"Reverted to turn {turn}.";
                }
                else
                {
                    Message = "Undo to turn failed.";
                }
            }
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
        }
    }
}

