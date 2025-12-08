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
                CurrentGame = MapFromGrpc(grpcGame);
                Message = $"Game Started (via gRPC, {difficulty})! Good luck.";
            }
            else
            {
                var request = new CreateGameRequest { Difficulty = difficulty, GridSize = gridSize };
                var response = await _httpClient.PostAsJsonAsync("/game", request);
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

    private GameStatus MapFromGrpc(GrpcGameStatus grpcGame)
    {
        var game = new GameStatus
        {
            GameId = grpcGame.GameId,
            Winner = string.IsNullOrEmpty(grpcGame.Winner) ? null : grpcGame.Winner,
            LastAttackResult = string.IsNullOrEmpty(grpcGame.LastAttackResult) ? null : grpcGame.LastAttackResult,
            LastAiAttackResult = string.IsNullOrEmpty(grpcGame.LastAiAttackResult) ? null : grpcGame.LastAiAttackResult,
            State = (GameState)grpcGame.State,
            History = new List<MoveHistory>()
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

        // Map History
        foreach (var h in grpcGame.History)
        {
            game.History.Add(new MoveHistory
            {
                Turn = h.Turn,
                PlayerMove = h.PlayerMove,
                PlayerResult = h.PlayerResult,
                AiMove = h.AiMove,
                AiResult = h.AiResult
            });
        }

        // Map Ships
        game.Ships = new List<ShipInfo>();
        foreach (var s in grpcGame.Ships)
        {
            game.Ships.Add(new ShipInfo
            {
                Letter = s.Letter[0],
                Size = s.Size,
                Row = s.Row,
                Col = s.Col,
                IsHorizontal = s.IsHorizontal
            });
        }

        return game;
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
                CurrentGame = MapFromGrpc(grpcGame);
                Message = "Ships placed! Battle starts (gRPC).";
            }
            else
            {
                if (CurrentGame.GameId == null) return;
                var request = new PlaceShipsRequest { GameId = Guid.Parse(CurrentGame.GameId), Ships = ships };
                var response = await _httpClient.PostAsJsonAsync($"/game/{CurrentGame.GameId}/place-ships", request);
                
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
                CurrentGame = MapFromGrpc(grpcGame);
            }
            else
            {
                var request = new AttackRequest { Row = row, Col = col };
                var response = await _httpClient.PostAsJsonAsync($"/game/{CurrentGame.GameId}/attack", request);
                
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

    public async Task UndoAsync()
    {
        if (CurrentGame == null) return;

        try
        {
            if (_useGrpc)
            {
                var request = new GrpcUndoRequest { GameId = CurrentGame.GameId ?? string.Empty };
                var grpcGame = await _grpcClient.UndoAsync(request);
                CurrentGame = MapFromGrpc(grpcGame);
            }
            else
            {
                var response = await _httpClient.PostAsync($"/game/{CurrentGame.GameId}/undo", null);
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
                CurrentGame = MapFromGrpc(grpcGame);
            }
            else
            {
                var response = await _httpClient.PostAsync($"/game/{CurrentGame.GameId}/undo-to-turn/{turn}", null);
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
