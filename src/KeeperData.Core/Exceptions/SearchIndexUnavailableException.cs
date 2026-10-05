namespace KeeperData.Core.Exceptions;

public sealed class SearchIndexUnavailableException : Exception
{
    public SearchIndexUnavailableException() : base("The holding search index is not available.") { }

    public SearchIndexUnavailableException(string message) : base(message) { }

    public SearchIndexUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}