using BattleShip.Models;
using FluentValidation;

namespace BattleShip.API.Validators;

public class PlaceShipsRequestValidator : AbstractValidator<PlaceShipsRequest>
{
    public PlaceShipsRequestValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("Game ID is required.");

        RuleFor(x => x.Ships)
            .NotEmpty().WithMessage("Ships list cannot be empty.")
            .Must(ships => ships.Count > 0).WithMessage("At least one ship must be placed.");
            
        RuleForEach(x => x.Ships).ChildRules(ship => {
            ship.RuleFor(s => s.Size).GreaterThan(0).WithMessage("Ship size must be greater than 0.");
            ship.RuleFor(s => s.Letter).NotEmpty().WithMessage("Ship letter is required.");
        });
    }
}
