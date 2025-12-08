using BattleShip.Models;

namespace BattleShip.App.Services;

public class ShipPlacementService
{
    public List<ShipInfo> PlaceShipsRandomly(List<ShipInfo> shipTemplates, int gridSize)
    {
        var random = new Random();
        var placedShips = new List<ShipInfo>();

        foreach (var shipTemplate in shipTemplates)
        {
            bool placed = false;
            while (!placed)
            {
                var ship = new ShipInfo 
                { 
                    Letter = shipTemplate.Letter, 
                    Size = shipTemplate.Size,
                    IsHorizontal = random.Next(2) == 0,
                    Row = random.Next(gridSize),
                    Col = random.Next(gridSize)
                };

                if (IsValidPlacement(ship, placedShips, gridSize))
                {
                    placedShips.Add(ship);
                    placed = true;
                }
            }
        }
        return placedShips;
    }

    public bool IsValidPlacement(ShipInfo newShip, List<ShipInfo> existingShips, int gridSize)
    {
        if (newShip.IsHorizontal)
        {
            if (newShip.Col + newShip.Size > gridSize) return false;
        }
        else
        {
            if (newShip.Row + newShip.Size > gridSize) return false;
        }

        foreach (var existing in existingShips)
        {
            if (ShipsOverlap(newShip, existing)) return false;
        }

        return true;
    }

    private bool ShipsOverlap(ShipInfo s1, ShipInfo s2)
    {
        var s1Coords = GetShipCoordinates(s1);
        var s2Coords = GetShipCoordinates(s2);
        return s1Coords.Intersect(s2Coords).Any();
    }

    private List<(int r, int c)> GetShipCoordinates(ShipInfo s)
    {
        var coords = new List<(int r, int c)>();
        for (int i = 0; i < s.Size; i++)
        {
            if (s.IsHorizontal) coords.Add((s.Row, s.Col + i));
            else coords.Add((s.Row + i, s.Col));
        }
        return coords;
    }
}
