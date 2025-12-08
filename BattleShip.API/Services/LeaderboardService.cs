using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BattleShip.Models;

namespace BattleShip.API.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private const string LeaderboardFilePath = "leaderboard.json";
        private static readonly object _fileLock = new object();

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
        {
            if (!File.Exists(LeaderboardFilePath))
            {
                return new List<LeaderboardEntry>();
            }

            string json;
            lock (_fileLock)
            {
                json = File.ReadAllText(LeaderboardFilePath);
            }
            
            var entries = JsonSerializer.Deserialize<List<LeaderboardEntry>>(json) ?? new List<LeaderboardEntry>();
            return entries.OrderByDescending(e => e.Wins).ToList();
        }

        public async Task AddWinAsync(string playerName)
        {
            var entries = await GetLeaderboardAsync();
            var entry = entries.FirstOrDefault(e => e.PlayerName == playerName);

            if (entry != null)
            {
                entry.Wins++;
            }
            else
            {
                entries.Add(new LeaderboardEntry { PlayerName = playerName, Wins = 1 });
            }

            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            lock (_fileLock)
            {
                File.WriteAllText(LeaderboardFilePath, json);
            }
        }
    }
}
