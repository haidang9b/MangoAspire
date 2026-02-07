using Mango.Core.Auth;
using Mango.RestApis.Requests;
using Mango.Web.Models;
using Mango.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Mango.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductsApi _productsApi;
    private readonly ICartApi _cartApi;
    private readonly ICurrentUserContext _currentUserContext;

    public HomeController(
        ILogger<HomeController> logger,
        IProductsApi productsApi,
        ICartApi cartApi,
        ICurrentUserContext currentUserContext
    )
    {
        _logger = logger;
        _productsApi = productsApi;
        _cartApi = cartApi;
        _currentUserContext = currentUserContext;
    }

    public async Task<IActionResult> Index(int pageIndex = 1, int pageSize = 9)
    {
        var result = await _productsApi.GetProductsAsync(pageIndex, pageSize);
        if (result != null && !result.IsError && result.Data != null)
        {
            return View(result.Data);
        }
        return View(new PaginatedItemsDto<ProductDto>());
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Details(Guid productId)
    {
        var result = await _productsApi.GetProductByIdAsync(productId);

        if (result != null && !result.IsError)
        {
            return View(result.Data);
        }
        return NotFound();
    }

    [HttpPost]
    [ActionName(nameof(Details))]
    [Authorize]
    public async Task<IActionResult> DetailsPost(ProductDto productDto)
    {
        var addToCartDto = new AddToCartRequestDto
        {
            ProductId = productDto.Id,
            Count = productDto.Count,
            CouponCode = string.Empty,
        };


        var addToCartResponse = await _cartApi.AddToCartAsync(addToCartDto);
        if (addToCartResponse != null && !addToCartResponse.IsError)
        {
            return RedirectToAction(nameof(Index));
        }
        return View(productDto);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Authorize]
    public async Task<IActionResult> Login()
    {
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Logout()
    {
        return SignOut("Cookies", "oidc");
    }
}
