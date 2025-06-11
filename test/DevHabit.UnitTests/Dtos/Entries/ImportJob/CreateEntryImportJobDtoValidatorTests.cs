using DevHabit.Api.Dtos.Entries.ImportJob;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace DevHabit.UnitTests.Dtos.Entries.ImportJob;

public sealed class CreateEntryImportJobDtoValidatorTests
{
    private readonly CreateEntryImportJobDtoValidator _sut = new();

    [Fact]
    public async Task Validate_ShouldNotReturnError_WhenAllPropertiesAreValid()
    {
        // Arrange
        CreateEntryImportJobDto dto = new()
        {
            File = CreateFormFile("test.csv", "text/csv", 1024),
        };

        // Act
        TestValidationResult<CreateEntryImportJobDto> result = await _sut.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenFileIsNotCsv()
    {
        // Arrange
        CreateEntryImportJobDto dto = new()
        {
            File = CreateFormFile("test.txt", "text/plain", 1024),
        };

        // Act
        TestValidationResult<CreateEntryImportJobDto> result = await _sut.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.File.FileName);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenFileExceedsMaxSize()
    {
        // Arrange
        CreateEntryImportJobDto dto = new()
        {
            File = CreateFormFile("test.csv", "text/csv", 11 * 1024 * 1024), // 11MB
        };

        // Act
        TestValidationResult<CreateEntryImportJobDto> result = await _sut.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.File.Length);
    }

    private static IFormFile CreateFormFile(string filename, string contentType, int length)
    {
        IFormFile formFile = Substitute.For<IFormFile>();

        formFile.FileName.Returns(filename);
        formFile.ContentType.Returns(contentType);
        formFile.Length.Returns(length);

        return formFile;
    }
}
