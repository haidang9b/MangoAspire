namespace Mango.Core.Auth;

public interface ICurrentUserContext
{
    string? UserId { get; set; }

    bool IsAuthenticated { get; set; }
}
