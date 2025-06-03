using FluentValidation;

namespace DevHabit.Api.Dtos.Auth;

public sealed class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
{
    public LoginUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email must not be empty")
            .MaximumLength(300)
            .WithMessage("Email can not exceed 300 characters")
            .EmailAddress()
            .WithMessage("Must be a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password must not be empty");
    }
}
