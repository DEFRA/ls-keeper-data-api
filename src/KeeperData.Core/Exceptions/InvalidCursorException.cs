using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Core.Exceptions;

/// <summary>
/// Raised when a supplied pagination cursor cannot be decoded. Maps to HTTP 400.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InvalidCursorException : DomainException
{
    public const string ParameterName = "Cursor";
    public const string DefaultMessage =
        "The cursor is invalid. Omit the cursor to start from the first page, or supply a nextCursor value returned by a previous response.";

    public override string Title => "Invalid Cursor";
    public InvalidCursorException() : base(DefaultMessage) { }
    public InvalidCursorException(string message) : base(message) { }
    public InvalidCursorException(string message, Exception innerException)
        : base(message, innerException) { }
}