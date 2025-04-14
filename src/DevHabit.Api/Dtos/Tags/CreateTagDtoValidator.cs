using FluentValidation;

namespace DevHabit.Api.Dtos.Tags;

public sealed class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(3, 50)
            .WithName(x => nameof(x.Name).ToLowerInvariant());

        RuleFor(x => x.Description)
            .MaximumLength(50)
            .WithName(x => nameof(x.Description).ToLowerInvariant());
    }
}
