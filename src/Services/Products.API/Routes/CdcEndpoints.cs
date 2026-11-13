using Mango.Core.Auth;
using Products.API.Features.Cdc;

namespace Products.API.Routes;

/// <summary>
/// Operational endpoints for the change-data-capture pipeline.
/// </summary>
public static class CdcEndpoints
{
    extension(WebApplication routeGroupBuilder)
    {
        public RouteGroupBuilder MapCdcApi()
        {
            // Administrative: re-emitting every row is expensive and floods every CDC consumer.
            // The path sits under /api/products, so the gateway's products route exposes it too.
            var group = routeGroupBuilder.MapGroup("/api/products/cdc-snapshots")
                .WithTags("CDC")
                .RequireAuthorization(AuthPolicies.AdminOnly);

            // Requests a re-read of the captured tables, re-emitting every row into the CDC
            // stream. Use when a consumer needs history that has aged out of the stream's
            // retention window — onboarding a service long after the fact, or adding a table
            // to the capture list. Unlike dropping the replication slot, this leaves
            // up-to-date consumers untouched.
            group.MapPost("/", async (RequestIncrementalSnapshot.Command command, ISender sender) =>
            {
                var result = await sender.SendAsync(command);
                return Results.Accepted("/api/products/cdc-snapshots", result);
            })
            .WithSummary("Request an incremental CDC snapshot")
            .WithDescription(
                "Signals Debezium to re-read the given captured tables and re-emit their rows into " +
                "the CDC stream, so consumers can rebuild read-models beyond the stream's retention " +
                "window. Returns as soon as the signal is recorded; the snapshot runs asynchronously.");

            return group;
        }
    }
}
