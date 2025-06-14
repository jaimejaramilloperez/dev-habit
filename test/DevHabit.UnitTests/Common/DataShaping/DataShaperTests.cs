using System.Dynamic;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Common;
using Microsoft.AspNetCore.Http;

namespace DevHabit.UnitTests.Common.DataShaping;

public sealed class DataShaperTests
{
    private sealed record TestDto
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public int Value { get; init; }
    }

    [Fact]
    public void ShapeData_ShouldReturnAllProperties_WhenFieldsAreNull()
    {
        // Arrange
        TestDto dto = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test",
            Description = "Description",
            Value = 33,
        };

        // Act
        ExpandoObject result = DataShaper.ShapeData(dto, null);

        // Assert
        IDictionary<string, object?> dict = result;

        Assert.Equal(4, dict.Count);
        Assert.Equal(dto.Id, dict["Id"]);
        Assert.Equal(dto.Name, dict["Name"]);
        Assert.Equal(dto.Description, dict["Description"]);
        Assert.Equal(dto.Value, dict["Value"]);
    }

    [Fact]
    public void ShapeData_ShouldReturnRequestedProperties_WhenFieldsAreSpecified()
    {
        // Arrange
        TestDto dto = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test",
            Description = "Description",
            Value = 33,
        };

        // Act
        ExpandoObject result = DataShaper.ShapeData(dto, "id,name");

        // Assert
        IDictionary<string, object?> dict = result;

        Assert.Equal(2, dict.Count);
        Assert.Equal(dto.Id, dict["Id"]);
        Assert.Equal(dto.Name, dict["Name"]);
        Assert.False(dict.ContainsKey("Description"));
        Assert.False(dict.ContainsKey("Value"));
    }

    [Fact]
    public void ShapeData_ShouldBeCaseInsensitive_WhenMatchingFields()
    {
        // Arrange
        TestDto dto = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test",
            Description = "Description",
            Value = 33,
        };

        // Act
        ExpandoObject result = DataShaper.ShapeData(dto, "ID,NAME");

        // Assert
        IDictionary<string, object?> dict = result;

        Assert.Equal(2, dict.Count);
        Assert.Equal(dto.Id, dict["Id"]);
        Assert.Equal(dto.Name, dict["Name"]);
        Assert.False(dict.ContainsKey("Description"));
        Assert.False(dict.ContainsKey("Value"));
    }

    [Fact]
    public void ShapeCollectionData_ShouldReturnAllProperties_WhenFieldsAreNull()
    {
        // Arrange
        List<TestDto> dtos =
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test",
                Description = "Description",
                Value = 33,
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test2",
                Description = "Description2",
                Value = 42,
            },
        ];

        // Act
        IReadOnlyCollection<ExpandoObject> result = DataShaper.ShapeCollectionData(dtos, null);

        // Assert
        Assert.Equal(2, result.Count);

        IDictionary<string, object?> firstItem = result.First();

        Assert.Equal(4, firstItem.Count);
        Assert.Equal(dtos[0].Id, firstItem["Id"]);
        Assert.Equal(dtos[0].Name, firstItem["Name"]);
        Assert.Equal(dtos[0].Description, firstItem["Description"]);
        Assert.Equal(dtos[0].Value, firstItem["Value"]);
    }

    [Fact]
    public void ShapeCollectionData_ShouldReturnRequestedProperties_WhenFieldsAreSpecified()
    {
        // Arrange
        List<TestDto> dtos =
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test",
                Description = "Description",
                Value = 33,
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test2",
                Description = "Description2",
                Value = 42,
            },
        ];

        // Act
        IReadOnlyCollection<ExpandoObject> result = DataShaper.ShapeCollectionData(dtos, "id,name");

        // Assert
        Assert.Equal(2, result.Count);

        IDictionary<string, object?> firstItem = result.First();

        Assert.Equal(2, firstItem.Count);
        Assert.Equal(dtos[0].Id, firstItem["Id"]);
        Assert.Equal(dtos[0].Name, firstItem["Name"]);
        Assert.False(firstItem.ContainsKey("Description"));
        Assert.False(firstItem.ContainsKey("Value"));
    }

    [Fact]
    public void ShapeCollectionData_ShouldBeCaseInsensitive_WhenMatchingFields()
    {
        // Arrange
        List<TestDto> dtos =
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test",
                Description = "Description",
                Value = 33,
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test2",
                Description = "Description2",
                Value = 42,
            },
        ];

        // Act
        IReadOnlyCollection<ExpandoObject> result = DataShaper.ShapeCollectionData(dtos, "ID,NAME");

        // Assert
        Assert.Equal(2, result.Count);

        IDictionary<string, object?> firstItem = result.First();

        Assert.Equal(2, firstItem.Count);
        Assert.Equal(dtos[0].Id, firstItem["Id"]);
        Assert.Equal(dtos[0].Name, firstItem["Name"]);
        Assert.False(firstItem.ContainsKey("Description"));
        Assert.False(firstItem.ContainsKey("Value"));
    }

    [Fact]
    public void ShapeCollectionData_ShouldIncludeLinks_WhenLinksFactoryIsProvided()
    {
        // Arrange
        List<TestDto> dtos =
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test",
                Description = "Description",
                Value = 33,
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test2",
                Description = "Description2",
                Value = 42,
            },
        ];

        static List<LinkDto> CreateLinks(TestDto dto) =>
        [
            new($"test/{dto.Id}", LinkRelations.Self, HttpMethods.Get),
        ];

        // Act
        IReadOnlyCollection<ExpandoObject> result = DataShaper.ShapeCollectionData(dtos, null, CreateLinks);

        // Assert
        Assert.Equal(2, result.Count);

        IDictionary<string, object?> firstItem = result.First();
        Assert.True(firstItem.ContainsKey(HateoasPropertyNames.Links));

        List<LinkDto>? links = (List<LinkDto>)firstItem[HateoasPropertyNames.Links]!;

        Assert.NotNull(links);
        Assert.Single(links);
        Assert.Equal($"test/{dtos[0].Id}", links[0].Href);
        Assert.Equal(LinkRelations.Self, links[0].Rel);
        Assert.Equal(HttpMethods.Get, links[0].Method);
    }
}
