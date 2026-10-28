using KeeperData.Core.Exceptions;
using System.Text;

namespace KeeperData.Application.Queries.Pagination;

public static class CursorHelper
{
    private const char Delimiter = '|';

    // Cursors are always produced from UTF-8 text, so reject byte sequences that are not valid UTF-8
    // rather than silently substituting replacement characters.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Encode(string? sortValue, string id)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes($"{sortValue ?? ""}{Delimiter}{id}");
        return Convert.ToBase64String(plainTextBytes);
    }

    /// <summary>
    /// Returns true when no cursor has been supplied (null, empty or whitespace-only).
    /// </summary>
    public static bool IsAbsent(string? cursor) => string.IsNullOrWhiteSpace(cursor);

    /// <summary>
    /// Returns true when the cursor is absent or can be decoded.
    /// </summary>
    public static bool IsValid(string? cursor) => IsAbsent(cursor) || TryDecode(cursor, out _);

    /// <summary>
    /// Attempts to decode a supplied cursor. Returns false for an absent or malformed cursor.
    /// </summary>
    public static bool TryDecode(string? cursor, out (string sortValue, string id) decoded)
    {
        decoded = default;
        if (IsAbsent(cursor)) return false;

        string plainText;
        try
        {
            var base64EncodedBytes = Convert.FromBase64String(cursor!);
            plainText = StrictUtf8.GetString(base64EncodedBytes);
        }
        catch (Exception ex) when (ex is FormatException or DecoderFallbackException or ArgumentException)
        {
            return false;
        }

        // The sort value may itself contain the delimiter, so split on the last occurrence.
        var lastDelimiter = plainText.LastIndexOf(Delimiter);
        if (lastDelimiter == -1) return false;

        var id = plainText[(lastDelimiter + 1)..];
        if (string.IsNullOrWhiteSpace(id)) return false;

        decoded = (plainText[..lastDelimiter], id);
        return true;
    }

    /// <summary>
    /// Decodes a cursor. Returns null when no cursor was supplied.
    /// </summary>
    /// <exception cref="InvalidCursorException">The supplied cursor is malformed.</exception>
    public static (string sortValue, string id)? Decode(string? cursor)
    {
        if (IsAbsent(cursor)) return null;

        if (!TryDecode(cursor, out var decoded))
            throw new InvalidCursorException();

        return decoded;
    }
}