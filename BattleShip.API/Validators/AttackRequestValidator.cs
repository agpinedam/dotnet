using BattleShip.Models;
using FluentValidation;

namespace BattleShip.API.Validators;

public class AttackRequestValidator : AbstractValidator<AttackRequest>
{
    public AttackRequestValidator()
    {
        RuleFor(x => x.Row)
            .InclusiveBetween(0, 9).WithMessage("Row must be between 0 and 9.");

        RuleFor(x => x.Col)
            .InclusiveBetween(0, 9).WithMessage("Column must be between 0 and 9.");
    }
}
