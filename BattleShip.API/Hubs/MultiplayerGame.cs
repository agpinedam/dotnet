using System.Collections.Generic;
using System.Linq;
using BattleShip.Models;

namespace BattleShip.API.Hubs
{
    // Represents the state of a single multiplayer game
    public class MultiplayerGame
    {
        public string GameId { get; }
        public Player Player1 { get; }
        public Player? Player2 { get; set; }
        public Player CurrentTurnPlayer { get; set; }
        public List<MoveHistory> History { get; set; } = new List<MoveHistory>();

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
}
