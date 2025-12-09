using BattleShip.Models;
using FluentValidation;

namespace BattleShip.API.Validators;

public class AttackRequestValidator : AbstractValidator<AttackRequest>
{
    public AttackRequestValidator()
    {
        RuleFor(x => x.Row)
            .GreaterThanOrEqualTo(0).WithMessage("Row must be positive.");

        RuleFor(x => x.Col)
            .GreaterThanOrEqualTo(0).WithMessage("Column must be positive.");
    }
}
