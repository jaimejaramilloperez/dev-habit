using FluentValidation;

namespace DevHabit.Api.Dtos.Habits;

public sealed class HabitsQueryParametersValidator : AbstractValidator<HabitsQueryParameters>
{
    public HabitsQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize).InclusiveBetween(1, 50)
            .WithMessage("Page size must be between 1 and 50");
    }
}
