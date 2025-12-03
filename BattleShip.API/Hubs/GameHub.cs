using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleShip.Models;

namespace BattleShip.API.Hubs
{
    // Represents a player in a multiplayer game
    public class Player
    {
        public string ConnectionId { get; }
        public string Name { get; }
        public char[][] Grid { get; }
        public List<ShipInfo> Ships { get; set; } = new List<ShipInfo>();
        public bool ShipsPlaced { get; set; } = false;

        public Player(string connectionId, string name, int gridSize)
        {
            ConnectionId = connectionId;
            Name = name;
            Grid = new char[gridSize][];
            for (int i = 0; i < gridSize; i++)
            {
                Grid[i] = new char[gridSize];
                for (int j = 0; j < gridSize; j++)
                {
                    Grid[i][j] = ' '; // Initialize with empty space
                }
            }
        }
    }

    // Represents the state of a single multiplayer game
    public class MultiplayerGame
    {
        public string GameId { get; }
        public Player Player1 { get; }
        public Player? Player2 { get; set; }
        public Player CurrentTurnPlayer { get; set; }

        public MultiplayerGame(string gameId, Player player1)
        {
            GameId = gameId;
            Player1 = player1;
            CurrentTurnPlayer = player1;
        }

        public GameState GetState()
        {
            if (Player2 == null || !Player1.ShipsPlaced || !Player2.ShipsPlaced)
            {
                return GameState.Setup;
            }
            // Simplified win condition
            if (IsLoser(Player1)) return GameState.GameOver;
            if (IsLoser(Player2)) return GameState.GameOver;
            
            return GameState.Playing;
        }

        public bool IsLoser(Player player)
        {
            // A player loses if all their ship cells have been hit
            return player.Ships.All(ship => 
                ship.Hits >= ship.Size
            );
        }
    }

    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, MultiplayerGame> _games = new ConcurrentDictionary<string, MultiplayerGame>();
        private static readonly ConcurrentDictionary<string, string> _connectionToGame = new ConcurrentDictionary<string, string>();

        public async Task CreateOrJoinGame(string gameId, string playerName, int gridSize = 10)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerName))
            {
                await Clients.Caller.SendAsync("Error", "Game ID and Player Name cannot be empty.");
                return;
            }

            var player = new Player(Context.ConnectionId, playerName, gridSize)
            {
                Ships = GetDefaultShips()
            };

            if (_games.TryGetValue(gameId, out var game))
            {
                // Game exists, join as Player 2
                if (game.Player2 != null)
                {
                    await Clients.Caller.SendAsync("Error", "This game is already full.");
                    return;
                }
                game.Player2 = player;
                _connectionToGame[Context.ConnectionId] = gameId;
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
                await SendGameState(gameId);
            }
            else
            {
                // Game does not exist, create it
                var newGame = new MultiplayerGame(gameId, player);
                if (_games.TryAdd(gameId, newGame))
                {
                    _connectionToGame[Context.ConnectionId] = gameId;
                    await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
                    await SendGameState(gameId);
                }
            }
        }

        public async Task PlaceShips(List<ShipInfo> ships)
        {
            if (_connectionToGame.TryGetValue(Context.ConnectionId, out var gameId) && _games.TryGetValue(gameId, out var game))
            {
                var player = (game.Player1.ConnectionId == Context.ConnectionId) ? game.Player1 : game.Player2;
                if (player == null) return;

                player.Ships = ships;
                foreach (var ship in ships)
                {
                    for (int i = 0; i < ship.Size; i++)
                    {
                        int row = ship.IsHorizontal ? ship.Row : ship.Row + i;
                        int col = ship.IsHorizontal ? ship.Col + i : ship.Col;
                        if(row < player.Grid.Length && col < player.Grid[0].Length)
                        {
                            player.Grid[row][col] = ship.Letter;
                        }
                    }
                }
                player.ShipsPlaced = true;
                await SendGameState(gameId);
            }
        }

        public async Task Attack(int row, int col)
        {
            if (_connectionToGame.TryGetValue(Context.ConnectionId, out var gameId) && _games.TryGetValue(gameId, out var game))
            {
                if (game.CurrentTurnPlayer.ConnectionId != Context.ConnectionId || game.GetState() != GameState.Playing)
                {
                    return; // Not your turn or game not ready
                }

                var opponent = (game.Player1.ConnectionId == Context.ConnectionId) ? game.Player2 : game.Player1;
                if (opponent == null) return;

                char cell = opponent.Grid[row][col];
                if (char.IsLetter(cell))
                {
                    opponent.Grid[row][col] = 'X'; // Hit
                    var hitShip = opponent.Ships.FirstOrDefault(s => s.Letter == cell);
                    if(hitShip != null) hitShip.Hits++;
                }
                else if (cell == ' ')
                {
                    opponent.Grid[row][col] = 'M'; // Miss
                }
                
                // Switch turns
                game.CurrentTurnPlayer = opponent;
                await SendGameState(gameId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionToGame.TryRemove(Context.ConnectionId, out var gameId) && _games.TryGetValue(gameId, out var game))
            {
                // Notify other player that their opponent has disconnected
                await Clients.OthersInGroup(gameId).SendAsync("OpponentDisconnected");
                _games.TryRemove(gameId, out _); // Clean up the game
            }
            await base.OnDisconnectedAsync(exception);
        }

        private async Task SendGameState(string gameId)
        {
            if (_games.TryGetValue(gameId, out var game))
            {
                // Create and send a tailored GameStatus object to each player
                var p1Status = CreatePlayerStatus(game, game.Player1, game.Player2);
                await Clients.Client(game.Player1.ConnectionId).SendAsync("UpdateGameState", p1Status);

                if (game.Player2 != null)
                {
                    var p2Status = CreatePlayerStatus(game, game.Player2, game.Player1);
                    await Clients.Client(game.Player2.ConnectionId).SendAsync("UpdateGameState", p2Status);
                }
            }
        }

        private GameStatus CreatePlayerStatus(MultiplayerGame game, Player me, Player? opponent)
        {
            var gridSize = me.Grid.Length;
            return new GameStatus
            {
                GameId = game.GameId,
                State = game.GetState(),
                PlayerGrid = me.Grid,
                OpponentGrid = opponent != null ? MaskOpponentGrid(opponent.Grid) : CreateEmptyBoolGrid(gridSize),
                Ships = me.Ships,
                IsMyTurn = game.CurrentTurnPlayer.ConnectionId == me.ConnectionId,
                IsGameOver = game.GetState() == GameState.GameOver,
                Winner = (game.GetState() == GameState.GameOver) ? (game.IsLoser(me) ? opponent?.Name : me.Name) : null,
                ShipsPlaced = me.ShipsPlaced
            };
        }

        private bool?[][] CreateEmptyBoolGrid(int size)
        {
            var grid = new bool?[size][];
            for(int i = 0; i < size; i++)
            {
                grid[i] = new bool?[size];
            }
            return grid;
        }

        private bool?[][] MaskOpponentGrid(char[][] grid)
        {
            int size = grid.Length;
            var maskedGrid = new bool?[size][];
            for (int i = 0; i < size; i++)
            {
                maskedGrid[i] = new bool?[size];
                for (int j = 0; j < size; j++)
                {
                    if (grid[i][j] == 'X') maskedGrid[i][j] = true;   // Hit
                    else if (grid[i][j] == 'M') maskedGrid[i][j] = false; // Miss
                    else maskedGrid[i][j] = null; // Not attacked
                }
            }
            return maskedGrid;
        }
        
        private List<ShipInfo> GetDefaultShips() => new List<ShipInfo>
        {
            new() { Letter = 'A', Size = 5 }, new() { Letter = 'B', Size = 4 },
            new() { Letter = 'C', Size = 3 }, new() { Letter = 'D', Size = 3 },
            new() { Letter = 'E', Size = 2 }
        };
    }
}
