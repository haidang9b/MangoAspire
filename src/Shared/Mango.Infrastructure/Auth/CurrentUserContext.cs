using Mango.Core.Auth;

namespace Mango.Infrastructure.Auth;

public class CurrentUserContext : ICurrentUserContext
{
    public string? UserId { get; set; }

    public bool IsAuthenticated { get; set; }
}
