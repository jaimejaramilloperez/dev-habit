using FluentValidation;

namespace DevHabit.Api.Dtos.Entries;

public sealed class CreateEntryBatchDtoValidator : AbstractValidator<CreateEntryBatchDto>
{
    public CreateEntryBatchDtoValidator(/*CreateEntryValidator entryValidator*/)
    {
        RuleFor(x => x.Entries)
            .NotEmpty()
            .WithMessage("At least one entry is required")
            .Must(entries => entries.Count <= 20)
            .WithMessage("Maximum of 20 entries per batch");

        // RuleForEach(x => x.Entries)
        //     .SetValidator(entryValidator);
    }
}
