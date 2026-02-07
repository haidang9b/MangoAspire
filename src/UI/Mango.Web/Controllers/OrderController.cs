using Mango.Web.Models;
using Mango.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Web.Controllers;

[Authorize]
public class OrderController(IOrdersApi ordersApi) : Controller
{
    public async Task<IActionResult> Index(int pageIndex = 1, int pageSize = 10)
    {
        var result = await ordersApi.GetUserOrdersAsync(pageIndex, pageSize);
        if (!result.IsError && result.Data != null)
        {
            return View(result.Data);
        }

        return View(new PaginatedItemsDto<OrderDto>());
    }


    public async Task<IActionResult> Details(Guid id)
    {
        var result = await ordersApi.GetOrderByIdAsync(id);
        if (!result.IsError)
        {
            return View(result.Data);
        }

        TempData["error"] = "Order not found";
        return RedirectToAction(nameof(Index));
    }
}
