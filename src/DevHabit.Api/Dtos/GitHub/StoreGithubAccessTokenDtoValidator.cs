using FluentValidation;

namespace DevHabit.Api.Dtos.GitHub;

public sealed class StoreGithubAccessTokenDtoValidator : AbstractValidator<StoreGithubAccessTokenDto>
{
    public StoreGithubAccessTokenDtoValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithMessage("Access token must not be empty");

        RuleFor(x => x.ExpiresInDays)
            .GreaterThan(0)
            .WithMessage("Expires in days must be greater than 0");
    }
}
