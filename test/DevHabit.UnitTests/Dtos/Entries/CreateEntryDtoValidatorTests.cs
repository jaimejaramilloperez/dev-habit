using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Entities;
using FluentValidation.Results;

namespace DevHabit.UnitTests.Dtos.Entries;

public sealed class CreateEntryDtoValidatorTests
{
    private readonly CreateEntryDtoValidator _sut = new();

    [Fact]
    public async Task Validate_ShouldSucceed_WhenInputDtoIsValid()
    {
        // Arrange
        CreateEntryDto dto = new()
        {
            HabitId = Habit.CreateNewId(),
            Value = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenHabitIdIsEmpty()
    {
        // Arrange
        CreateEntryDto dto = new()
        {
            HabitId = string.Empty,
            Value = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
        };

        // Act
        ValidationResult result = await _sut.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        ValidationFailure validationFailure = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateEntryDto.HabitId), validationFailure.PropertyName);
    }
}
