using System.Collections.Concurrent;

namespace ChatAgent.App.Services;

public class ProductMemoryStorage : IProductMemoryStorage
{
    private readonly IProductsApi _productsApi;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, ProductDto> _productsById = new();
    private readonly List<(ProductDto Product, ReadOnlyMemory<float> Embedding)> _productVectors = [];
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public ProductMemoryStorage(IProductsApi productsApi)
    {
        _productsApi = productsApi;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync(bool forceRefresh = false)
    {
        // Check if cache needs refresh
        if (forceRefresh || _productsById.Count == 0 || DateTime.UtcNow - _lastRefresh > _cacheExpiration)
        {
            await RefreshCacheAsync();
        }

        return _productsById.Values;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid productId)
    {
        await GetProductsAsync(); // Ensure cache is loaded
        _productsById.TryGetValue(productId, out var product);
        return product;
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetProductsAsync();

        // Ensure cache is loaded
        await GetProductsAsync();

        // Calculate similarity scores and rank products
        //var rankedProducts = _productVectors
        //    .Select(pv => new
        //    {
        //        Product = pv.Product,
        //        Similarity = CosineSimilarity(searchEmbedding, pv.Embedding)
        //    })
        //    .OrderByDescending(x => x.Similarity)
        //    .Where(x => x.Similarity > 0.7) // Similarity threshold
        //    .Select(x => x.Product)
        //    .ToList();

        //// If no semantic matches, fall back to keyword search
        //if (rankedProducts.Count == 0)
        //{
        //    return _productsById.Values.Where(p =>
        //        p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //        p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //        p.CategoryName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        //    );
        //}

        //return rankedProducts;
        return _productsById.Values.Where(p =>
               p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               p.CategoryName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
           );
    }

    private async Task RefreshCacheAsync()
    {
        // Use semaphore to prevent multiple simultaneous refreshes
        await _refreshLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (DateTime.UtcNow - _lastRefresh <= _cacheExpiration && _productsById.Count > 0)
                return;

            var hasNextPage = true;

            var products = new List<ProductDto>();
            while (hasNextPage)
            {
                var result = await _productsApi.GetProductsAsync(pageSize: 50);
                if (result.Data?.Data == null)
                    return;

                products.AddRange(result.Data.Data);

                hasNextPage = result.Data.HasNextPage;
            }


            // Clear existing data
            _productsById.Clear();
            _productVectors.Clear();

            // Generate embeddings for all products
            foreach (var product in products)
            {
                //    // Create searchable text from product details
                //    var searchableText = $"{product.Name} {product.Description} {product.CategoryName}";
                //    var embedding = await _embeddingService.GenerateEmbeddingAsync(searchableText);

                _productsById[product.Id] = product;
                //_productVectors.Add((product, embedding));
            }

            _lastRefresh = DateTime.UtcNow;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task ForceRefreshAsync()
    {
        await RefreshCacheAsync();
    }

    private static float CosineSimilarity(ReadOnlyMemory<float> vector1, ReadOnlyMemory<float> vector2)
    {
        var v1 = vector1.Span;
        var v2 = vector2.Span;

        if (v1.Length != v2.Length)
            return 0;

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            magnitude1 += v1[i] * v1[i];
            magnitude2 += v2[i] * v2[i];
        }

        var magnitude = MathF.Sqrt(magnitude1) * MathF.Sqrt(magnitude2);
        return magnitude == 0 ? 0 : dotProduct / magnitude;
    }
}
