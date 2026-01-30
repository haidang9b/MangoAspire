using Mango.Core.Auth;
using Mango.Web.Models;
using Mango.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartApi _cartApi;
    private readonly ICouponsApi _couponsApi;

    private readonly ICurrentUserContext _currentUserContext;

    public CartController(ICartApi cartApi, ICouponsApi couponsApi, ICurrentUserContext currentUserContext)
    {
        _cartApi = cartApi;
        _couponsApi = couponsApi;
        _currentUserContext = currentUserContext;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        return View(await LoadCartDtoBasedOnLoggedInUser());
    }

    [Authorize]
    public async Task<IActionResult> Remove(Guid cartDetailsId)
    {
        var response = await _cartApi.RemoveFromCartAsync(cartDetailsId);

        if (response != null && !response.IsError)
        {
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
    {
        var response = await _cartApi.RemoveCouponAsync(cartDto.CartHeader.UserId);

        if (response != null && !response.IsError)
        {
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
    {
        var response = await _cartApi.ApplyCouponAsync(cartDto);

        if (response != null && !response.IsError)
        {
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        return View(await LoadCartDtoBasedOnLoggedInUser());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Checkout(CartDto cartDto)
    {
        try
        {
            var response = await _cartApi.CheckoutAsync(cartDto.CartHeader);
            if (response.IsError)
            {
                TempData["Error"] = response.ErrorMessage;
                return RedirectToAction(nameof(Checkout));
            }
            return RedirectToAction(nameof(Confirmation));
        }
        catch (Exception ex)
        {
            return View(cartDto);
        }
    }

    [Authorize]
    public async Task<IActionResult> Confirmation()
    {
        return View();
    }

    [NonAction]
    private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
    {
        var response = await _cartApi.GetCartByUserIdAsync(_currentUserContext.UserId);
        CartDto cartDto = new();
        if (response != null && !response.IsError)
        {
            cartDto = response.Data;
        }
        if (cartDto.CartHeader != null)
        {
            if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
            {
                var coupon = await _couponsApi.GetCouponAsync(cartDto.CartHeader.CouponCode);
                if (coupon != null && !coupon.IsError && coupon.Data != null)
                {
                    cartDto.CartHeader.DiscountTotal = coupon.Data.DiscountAmount;
                }
            }

            foreach (var detail in cartDto.CartDetails)
            {
                cartDto.CartHeader.OrderTotal += (detail.Product.Price * detail.Count);
            }

            cartDto.CartHeader.OrderTotal -= cartDto.CartHeader.DiscountTotal;
        }
        return cartDto;
    }
}
