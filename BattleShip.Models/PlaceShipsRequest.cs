using System.ComponentModel.DataAnnotations;

namespace BattleShip.Models;

public class PlaceShipsRequest
{
    [Required]
    public Guid GameId { get; set; }

    [Required]
    public List<ShipInfo> Ships { get; set; } = new();
}
