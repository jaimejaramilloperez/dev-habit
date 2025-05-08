using System.Dynamic;

namespace DevHabit.Api.Services;

public interface IDataShapingService
{
    ICollection<ExpandoObject> ShapeCollectionData<T>(ICollection<T> entities, string? fields);
    ExpandoObject ShapeData<T>(T entity, string? fields);
    bool AreAllFieldsValid<T>(string? fields);
}
