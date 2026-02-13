using Mango.Orchestrators.Data;
using Mango.ServiceDefaults;

namespace Mango.Orchestrators.Extensions;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddApiDefaults()
        {
            builder.AddServiceDefaults();

            builder.AddNpgsqlDataSource(connectionName: "sagaorchestratorsdb");

            builder.Services.AddServices(builder.Configuration);

            builder.EnrichNpgsqlDbContext<SagaDbContext>();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            return builder;
        }
    }
}
