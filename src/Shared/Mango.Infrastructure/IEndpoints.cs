using Microsoft.AspNetCore.Builder;

namespace Mango.Infrastructure;

public interface IEndpoints
{
    WebApplication MapEndpoints(WebApplication app);
}
