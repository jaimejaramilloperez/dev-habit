using FluentValidation;

namespace DevHabit.Api.Dtos.Entries;

public sealed class CreateEntryDtoValidator : AbstractValidator<CreateEntryDto>
{
    public CreateEntryDtoValidator()
    {
        RuleFor(x => x.HabitId)
            .NotEmpty()
            .WithMessage("Habit ID must not be empty");

        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value must not be empty")
            .GreaterThanOrEqualTo(0)
            .WithMessage("Value must be greater than or equal to 0");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date must not be empty")
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date can not be in the future");
    }
}
