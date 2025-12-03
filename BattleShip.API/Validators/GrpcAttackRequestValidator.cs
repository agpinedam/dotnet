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
            .GreaterThanOrEqualTo(0).WithMessage("Row must be positive.");

        RuleFor(x => x.Col)
            .GreaterThanOrEqualTo(0).WithMessage("Column must be positive.");
    }
}
