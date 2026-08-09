namespace Mango.Core.Exceptions;

/// <summary>
/// The caller is authenticated but is not entitled to the resource they named.
/// </summary>
/// <remarks>
/// Distinct from <see cref="DataVerificationException"/>, which is a 400 and means the request
/// itself was wrong. Use this only where the client explicitly identified a resource - a user id
/// in a route, say - so "not yours" is worth saying. Where the client named nothing, prefer a
/// not-found result instead: distinguishing "not yours" from "does not exist" there only tells an
/// attacker which identifiers are real.
/// </remarks>
public class ForbiddenException : Exception, IMangoException
{
    public ForbiddenException(string? message) : base(message) { }

    public ForbiddenException(string? message, Exception? innerException) : base(message, innerException) { }
}
