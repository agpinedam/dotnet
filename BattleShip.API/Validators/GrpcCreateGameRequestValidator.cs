using BattleShip.Protos;
using FluentValidation;

namespace BattleShip.API.Validators;

public class GrpcCreateGameRequestValidator : AbstractValidator<GrpcCreateGameRequest>
{
    public GrpcCreateGameRequestValidator()
    {
        RuleFor(x => x.GridSize)
            .InclusiveBetween(5, 20).WithMessage("Grid size must be between 5 and 20.");

        RuleFor(x => x.Difficulty)
            .InclusiveBetween(0, 2).WithMessage("Invalid difficulty level.");
    }
}
