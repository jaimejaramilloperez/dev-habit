using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace DevHabit.Api.Dtos.Entries;

public sealed record EntryCursorDto(string Id, DateOnly Date)
{
    public static string Encode(string id, DateOnly date)
    {
        EntryCursorDto cursor = new(id, date);
        string json = JsonConvert.SerializeObject(cursor);
        return Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
    }

    public static EntryCursorDto? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            string json = Base64UrlEncoder.Decode(cursor);
            return JsonConvert.DeserializeObject<EntryCursorDto>(json);
        }
        catch
        {
            return null;
        }
    }
}
