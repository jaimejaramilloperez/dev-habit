using FluentValidation;

namespace DevHabit.Api.Dtos.Entries.ImportJob;

public sealed class CreateEntryImportJobDtoValidator : AbstractValidator<CreateEntryImportJobDto>
{
    private const int MaxFileSizeInMegabytes = 10;
    private const int MaxFileSizeInBytes = MaxFileSizeInMegabytes * 1024 * 1024;

    public CreateEntryImportJobDtoValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required");

        RuleFor(x => x.File.Length)
            .LessThanOrEqualTo(MaxFileSizeInBytes)
            .WithMessage($"File size must be less than {MaxFileSizeInMegabytes}MB");

        RuleFor(x => x.File.FileName)
            .Must(fileName => fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("File must be a CSV file");
    }
}
