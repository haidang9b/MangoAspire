using ChatAgent.App.Data;
using Mango.Infrastructure.Extensions;
using Mango.ServiceDefaults;

namespace ChatAgent.App.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.Services.AddGlobalExceptionHandler();

        builder.AddNpgsqlDataSource(connectionName: "chatagentdb");
        builder.Services.AddServices(builder.Configuration);

        builder.EnrichNpgsqlDbContext<ChatAgentDbContext>();

        return builder;
    }
}
