using System.Collections.Generic;
using System.Threading.Tasks;
using BattleShip.Models;

namespace BattleShip.API.Services
{
    public interface ILeaderboardService
    {
        Task<List<LeaderboardEntry>> GetLeaderboardAsync();
        Task AddWinAsync(string playerName);
    }
}
