using System.Collections.Generic;
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
}
