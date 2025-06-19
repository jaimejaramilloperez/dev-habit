using FluentValidation;

namespace DevHabit.Api.Dtos.Entries;

public sealed class EntriesCursorParametersValidator : AbstractValidator<EntriesCursorParameters>
{
    public EntriesCursorParametersValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithMessage("Limit must be between 1 and 50");
    }
}
