using Microsoft.EntityFrameworkCore;

namespace Mango.Infrastructure;

public abstract class AppDbContextBase : DbContext, IDbFacadeResolver
{
    protected AppDbContextBase(DbContextOptions options) : base(options)
    {
    }
}
