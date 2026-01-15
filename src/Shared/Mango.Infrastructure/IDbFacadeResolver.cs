using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mango.Infrastructure;

public interface IDbFacadeResolver
{
    DatabaseFacade Database { get; }
}
