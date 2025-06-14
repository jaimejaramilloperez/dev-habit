using FluentValidation;

namespace DevHabit.Api.Dtos.Tags;

public sealed class UpdateTagDtoValidator : AbstractValidator<UpdateTagDto>
{
    public UpdateTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tag name must not be empty")
            .Length(3, 50)
            .WithMessage("Tag name must be between 3 and 50 characters");

        RuleFor(x => x.Description)
            .MaximumLength(50)
            .WithMessage("Tag description can not exceed 50 characters");
    }
}
