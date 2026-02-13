using ChatAgent.App.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ChatAgent.App.Plugins;

public class ProductsPlugin
{
    private readonly IProductsApi _productsApi;

    public ProductsPlugin(IProductsApi productsApi)
    {
        _productsApi = productsApi;
    }

    [KernelFunction]
    [Description("Get product details by id")]

    public async Task<IEnumerable<ProductDto>?> GetProductAsync()
    {
        var result = await _productsApi.GetProductsAsync();
        return result.Data?.Data;
    }

}
