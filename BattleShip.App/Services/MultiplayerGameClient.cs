using BattleShip.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BattleShip.App.Services
{
    public class MultiplayerGameClient : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly NavigationManager _navigationManager;
        private readonly IConfiguration _configuration;
        private TaskCompletionSource<bool>? _gameStateReceived;
        private string? _playerName;

        public GameStatus? CurrentGame { get; private set; }
        public string? Message { get; private set; }
        public event Action? OnChange;

        public MultiplayerGameClient(NavigationManager navigationManager, IConfiguration configuration)
        {
            _navigationManager = navigationManager;
            _configuration = configuration;
        }

        public async Task JoinGameAsync(string gameId, string playerName, int gridSize)
        {
            _playerName = playerName;
            _gameStateReceived = new TaskCompletionSource<bool>();
            var backendUrl = _configuration["BackendUrl"] ?? "http://localhost:5200";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri($"{backendUrl}/gamehub"))
                .Build();

            _hubConnection.On<GameStatus>("UpdateGameState", (gameStatus) =>
            {
                CurrentGame = gameStatus;
                UpdateMessage();
                _gameStateReceived?.TrySetResult(true);
                NotifyStateChanged();
            });

            _hubConnection.On<string>("Error", (errorMessage) =>
            {
                Message = errorMessage;
                _gameStateReceived?.TrySetResult(false);
                NotifyStateChanged();
            });

            _hubConnection.On("OpponentDisconnected", () => {
                Message = "Your opponent has disconnected. The game is over.";
                if (CurrentGame != null) CurrentGame.IsGameOver = true;
                NotifyStateChanged();
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.SendAsync("CreateOrJoinGame", gameId, playerName, gridSize);
                await _gameStateReceived.Task;
            }
            catch (Exception ex)
            {
                Message = $"Error connecting to server: {ex.Message}";
                NotifyStateChanged();
            }
        }

        public async Task LeaveGameAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
            CurrentGame = null;
            Message = null;
            NotifyStateChanged();
        }

        public async Task ConfirmPlacementAsync(List<ShipInfo> ships)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.SendAsync("PlaceShips", ships);
            }
        }

        public async Task AttackAsync(int row, int col)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.SendAsync("Attack", row, col);
            }
        }

        private void UpdateMessage()
        {
            if (CurrentGame == null) return;
            
            if (CurrentGame.IsGameOver)
            {
                if (CurrentGame.Winner == _playerName)
                {
                    Message = "You Won! Congratulations!";
                }
                else
                {
                    Message = "You Lost! Better luck next time.";
                }
                return;
            }

            switch (CurrentGame.State)
            {
                case GameState.Setup:
                    Message = CurrentGame.ShipsPlaced ? "Waiting for opponent..." : "Place your ships.";
                    break;
                case GameState.Playing:
                    // Display whose turn it is and the result of the last action
                    string turnMsg = CurrentGame.IsMyTurn ? "Your turn." : "Opponent's turn.";
                    string lastAction = !string.IsNullOrEmpty(CurrentGame.LastAttackResult) 
                        ? $" | Last: {CurrentGame.LastAttackResult}" 
                        : "";
                    
                    Message = $"{turnMsg}{lastAction}";
                    break;
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
