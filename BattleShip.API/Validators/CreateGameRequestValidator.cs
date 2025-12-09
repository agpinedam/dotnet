using BattleShip.Models;
using FluentValidation;

namespace BattleShip.API.Validators;

public class CreateGameRequestValidator : AbstractValidator<CreateGameRequest>
{
    public CreateGameRequestValidator()
    {
        RuleFor(x => x.GridSize)
            .InclusiveBetween(5, 20).WithMessage("Grid size must be between 5 and 20.");

        RuleFor(x => x.Difficulty)
            .IsInEnum().WithMessage("Invalid difficulty level.");
    }
}
