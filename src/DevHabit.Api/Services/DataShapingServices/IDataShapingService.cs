using System.Dynamic;
using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Services.DataShapingServices;

public interface IDataShapingService
{
    ExpandoObject ShapeData<T>(T entity, string? fields);

    ICollection<ExpandoObject> ShapeCollectionData<T>(
        ICollection<T> entities,
        string? fields,
        Func<T, ICollection<LinkDto>>? linksFactory = null);
}
