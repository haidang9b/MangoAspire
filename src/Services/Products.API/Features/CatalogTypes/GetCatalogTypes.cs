using Mango.Core.Caching;
using Microsoft.EntityFrameworkCore;
using Products.API.Dtos;

namespace Products.API.Features.CatalogTypes;

public class GetCatalogTypes
{
    private const string CacheKey = "CatalogTypes";

    private static readonly CacheEntryOptions CacheOptions = new() { Expiration = TimeSpan.FromHours(1) };

    public class Query : IQuery<List<CatalogTypeDto>>
    {
        internal class Handler(ProductDbContext dbContext, ICacheManager cacheManager) : IRequestHandler<Query, ResultModel<List<CatalogTypeDto>>>
        {
            public async Task<ResultModel<List<CatalogTypeDto>>> HandleAsync(Query request, CancellationToken cancellationToken)
            {
                var catalogTypes = await cacheManager.GetOrCreateAsync(
                    CacheKey,
                    dbContext,
                    static (db, ct) => new ValueTask<List<CatalogTypeDto>>(
                        db.CatalogTypes
                            .AsNoTracking()
                            .OrderBy(x => x.Type)
                            .Select(x => new CatalogTypeDto
                            {
                                Id = x.Id,
                                Type = x.Type
                            })
                            .ToListAsync(ct)),
                    CacheOptions,
                    cancellationToken: cancellationToken);

                return ResultModel<List<CatalogTypeDto>>.Create(catalogTypes);
            }
        }
    }
}
