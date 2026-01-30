using Mango.Core.Auth;
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

    public async Task<IActionResult> Index()
    {
        var result = await _productsApi.GetProductsAsync();
        if (result != null && !result.IsError)
        {
            return View(result.Data);
        }
        return View(new List<ProductDto>());
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
        CartDto cartDto = new()
        {
            CartHeader = new CartHeaderDto()
            {
                UserId = _currentUserContext.UserId
            }
        };

        CartDetailsDto cartDetails = new CartDetailsDto()
        {
            Count = productDto.Count,
            ProductId = productDto.Id,
        };

        var productResult = await _productsApi.GetProductByIdAsync(productDto.Id);
        if (productResult != null && !productResult.IsError)
        {
            cartDetails.Product = productResult.Data;
        }

        List<CartDetailsDto> cartDetailsDtos = new();
        cartDetailsDtos.Add(cartDetails);
        cartDto.CartDetails = cartDetailsDtos;

        var addToCartResponse = await _cartApi.AddToCartAsync(cartDto);
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
