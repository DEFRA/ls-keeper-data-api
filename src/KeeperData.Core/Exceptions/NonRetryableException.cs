namespace KeeperData.Core.Exceptions;

public class NonRetryableException : Exception
{
    public NonRetryableException(string message) : base(message)
    {
    }
}