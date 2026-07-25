using System.Diagnostics;

namespace OpenIdentity.App.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };

        // Surface OpenID Connect protocol errors (invalid client, bad redirect URI, ...)
        // when the request was rejected by the OpenIddict server middleware.
        var response = HttpContext.GetOpenIddictServerResponse();
        if (response != null)
        {
            model.Error = new ErrorMessage
            {
                Error = response.Error,
                ErrorDescription = response.ErrorDescription
            };
        }

        return View(model);
    }
}
