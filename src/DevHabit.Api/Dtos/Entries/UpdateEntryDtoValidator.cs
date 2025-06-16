using FluentValidation;

namespace DevHabit.Api.Dtos.Entries;

public sealed class UpdateEntryDtoValidator : AbstractValidator<UpdateEntryDto>
{
    public UpdateEntryDtoValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value must not be empty")
            .GreaterThanOrEqualTo(0)
            .WithMessage("Value must be greater than or equal to 0");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
