using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Products.API.Dtos;

namespace Products.API.Features.CatalogTypes;

public class GetCatalogTypes
{
    private const string CacheKey = "CatalogTypes";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public class Query : IQuery<List<CatalogTypeDto>>
    {
        internal class Handler(ProductDbContext dbContext, IMemoryCache memoryCache) : IRequestHandler<Query, ResultModel<List<CatalogTypeDto>>>
        {
            public async Task<ResultModel<List<CatalogTypeDto>>> HandleAsync(Query request, CancellationToken cancellationToken)
            {
                if (memoryCache.TryGetValue(CacheKey, out List<CatalogTypeDto>? cachedTypes) && cachedTypes != null)
                {
                    return ResultModel<List<CatalogTypeDto>>.Create(cachedTypes);
                }

                var catalogTypes = await dbContext.CatalogTypes
                    .AsNoTracking()
                    .OrderBy(x => x.Type)
                    .Select(x => new CatalogTypeDto
                    {
                        Id = x.Id,
                        Type = x.Type
                    })
                    .ToListAsync(cancellationToken);

                memoryCache.Set(CacheKey, catalogTypes, CacheDuration);

                return ResultModel<List<CatalogTypeDto>>.Create(catalogTypes);
            }
        }
    }
}
