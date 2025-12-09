using BattleShip.Protos;
using FluentValidation;

namespace BattleShip.API.Validators;

public class GrpcPlaceShipsRequestValidator : AbstractValidator<GrpcPlaceShipsRequest>
{
    public GrpcPlaceShipsRequestValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("Game ID is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid Game ID format.");

        RuleFor(x => x.Ships)
            .NotEmpty().WithMessage("Ships list cannot be empty.");
            
        RuleForEach(x => x.Ships).ChildRules(ship => {
            ship.RuleFor(s => s.Size).GreaterThan(0).WithMessage("Ship size must be greater than 0.");
            ship.RuleFor(s => s.Letter).NotEmpty().WithMessage("Ship letter is required.");
        });
    }
}
