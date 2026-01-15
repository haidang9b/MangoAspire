namespace Mango.Core.Exceptions;

public class DataVerificationException : Exception, IMangoException
{
    public DataVerificationException(string? message) : base(message) { }

    public DataVerificationException(string? message, Exception? innerException) : base(message, innerException) { }

    public static DataVerificationException Exception(string message) => new(message);
}
