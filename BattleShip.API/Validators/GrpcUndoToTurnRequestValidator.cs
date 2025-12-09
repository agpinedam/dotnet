using BattleShip.Protos;
using FluentValidation;

namespace BattleShip.API.Validators;

public class GrpcUndoToTurnRequestValidator : AbstractValidator<GrpcUndoToTurnRequest>
{
    public GrpcUndoToTurnRequestValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty().WithMessage("Game ID is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid Game ID format.");

        RuleFor(x => x.Turn)
            .GreaterThan(0).WithMessage("Turn must be greater than 0.");
    }
}
