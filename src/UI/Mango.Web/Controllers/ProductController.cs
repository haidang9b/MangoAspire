using Mango.Web.Models;
using Mango.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mango.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductsApi _productsApi;

    public ProductController(IProductsApi productsApi)
    {
        _productsApi = productsApi;
    }

    [Authorize]
    public async Task<IActionResult> ProductIndex(int pageIndex = 1, int pageSize = 10, int? catalogTypeId = null)
    {
        var result = await _productsApi.GetProductsAsync(pageIndex, pageSize, catalogTypeId);

        // Load catalog types for filter dropdown
        await LoadCatalogTypesAsync(catalogTypeId);

        if (result != null && !result.IsError && result.Data != null)
        {
            return View(result.Data);
        }
        return View(new PaginatedItemsDto<ProductDto>());
    }

    public async Task<IActionResult> ProductCreate()
    {
        await LoadCatalogTypesAsync(null);
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductCreate(ProductDto model)
    {
        if (ModelState.IsValid)
        {
            var result = await _productsApi.CreateProductAsync(model);
            if (result != null && !result.IsError)
            {
                return RedirectToAction(nameof(ProductIndex));
            }
        }
        await LoadCatalogTypesAsync(model.CatalogTypeId);
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProductEdit(Guid productId)
    {
        var result = await _productsApi.GetProductByIdAsync(productId);
        if (result != null && !result.IsError)
        {
            await LoadCatalogTypesAsync(result.Data?.CatalogTypeId);
            return View(result.Data);
        }
        return NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductEdit(ProductDto model)
    {
        if (ModelState.IsValid)
        {
            var result = await _productsApi.UpdateProductAsync(model);
            if (result != null && !result.IsError)
            {
                return RedirectToAction(nameof(ProductIndex));
            }
        }
        await LoadCatalogTypesAsync(model.CatalogTypeId);
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProductDelete(Guid productId)
    {
        var result = await _productsApi.GetProductByIdAsync(productId);
        if (result != null && !result.IsError)
        {
            return View(result.Data);
        }
        return NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductDelete(ProductDto model)
    {
        var result = await _productsApi.DeleteProductAsync(Guid.Parse(model.Id.ToString()));
        if (result != null && !result.IsError)
        {
            return RedirectToAction(nameof(ProductIndex));
        }
        return View(model);
    }

    private async Task LoadCatalogTypesAsync(int? selectedId)
    {
        var catalogTypesResult = await _productsApi.GetCatalogTypesAsync();
        var catalogTypes = catalogTypesResult?.Data ?? [];
        ViewBag.CatalogTypes = new SelectList(catalogTypes, "Id", "Type", selectedId);
    }
}
