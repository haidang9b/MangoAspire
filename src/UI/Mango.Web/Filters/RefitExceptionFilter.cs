using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Refit;

namespace Mango.Web.Filters;

public class RefitExceptionFilter : IAsyncExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly ILogger<RefitExceptionFilter> _logger;

    public RefitExceptionFilter(ITempDataDictionaryFactory tempDataFactory, ILogger<RefitExceptionFilter> logger)
    {
        _tempDataFactory = tempDataFactory;
        _logger = logger;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is ApiException apiException)
        {
            _logger.LogError(apiException, "Refit API call failed: {StatusCode} {Url}",
                apiException.StatusCode, apiException.Uri);

            var errorMessage = await TryExtractErrorMessageAsync(apiException)
                ?? $"Service error: {(int)apiException.StatusCode} {apiException.StatusCode}";

            var tempData = _tempDataFactory.GetTempData(context.HttpContext);
            tempData["Error"] = errorMessage;
            tempData.Save();

            // Redirect back to the referring page, or home if none
            var referer = context.HttpContext.Request.Headers.Referer.FirstOrDefault();
            context.Result = !string.IsNullOrEmpty(referer)
                ? new RedirectResult(referer)
                : new RedirectToActionResult("Index", "Home", null);

            context.ExceptionHandled = true;
        }
    }

    private static async Task<string?> TryExtractErrorMessageAsync(ApiException apiException)
    {
        try
        {
            if (apiException.HasContent)
            {
                // Try to read a ProblemDetails or { message } shaped response
                var content = await apiException.GetContentAsAsync<ProblemDetailsError>();
                if (!string.IsNullOrWhiteSpace(content?.Detail))
                    return content.Detail;
                if (!string.IsNullOrWhiteSpace(content?.Title))
                    return content.Title;
            }
        }
        catch
        {
            // fall through to default message
        }

        return apiException.ReasonPhrase;
    }

    private sealed record ProblemDetailsError(string? Title, string? Detail);
}
