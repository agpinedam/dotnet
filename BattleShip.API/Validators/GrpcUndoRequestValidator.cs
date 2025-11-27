using BattleShip.Protos;
using FluentValidation;

namespace BattleShip.API.Validators;

public class GrpcUndoRequestValidator : AbstractValidator<GrpcUndoRequest>
{
    public GrpcUndoRequestValidator()
    {
        RuleFor(x => x.GameId)
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid Game ID format.");
    }
}
