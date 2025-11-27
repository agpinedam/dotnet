using BattleShip.Protos;
using FluentValidation;

namespace BattleShip.API.Validators;

public class GrpcAttackRequestValidator : AbstractValidator<GrpcAttackRequest>
{
    public GrpcAttackRequestValidator()
    {
        RuleFor(x => x.GameId)
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid Game ID format.");

        RuleFor(x => x.Row)
            .InclusiveBetween(0, 9).WithMessage("Row must be between 0 and 9.");

        RuleFor(x => x.Col)
            .InclusiveBetween(0, 9).WithMessage("Column must be between 0 and 9.");
    }
}
