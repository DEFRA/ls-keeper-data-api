using System.Text;

namespace KeeperData.Application.Queries.Pagination;

public static class CursorHelper
{
    public static string Encode(string? sortValue, string id)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes($"{sortValue ?? ""}|{id}");
        return Convert.ToBase64String(plainTextBytes);
    }

    public static (string sortValue, string id)? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;

        try
        {
            var base64EncodedBytes = Convert.FromBase64String(cursor);
            var plainText = Encoding.UTF8.GetString(base64EncodedBytes);

            var lastDelimiter = plainText.LastIndexOf('|');
            if (lastDelimiter == -1) return null;

            var sortValue = plainText.Substring(0, lastDelimiter);
            var id = plainText.Substring(lastDelimiter + 1);

            return (sortValue, id);
        }
        catch
        {
            return null;
        }
    }
}