using Mango.Core.Exceptions;
using Mango.Core.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Coupons.API.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        _logger.LogError(
            exception, "Exception occurred: {Message}", exception.Message);
        var activity = Activity.Current;

        activity?.SetExceptionTags(exception);

        if (exception is ValidationException validationException)
        {
            await HandleValidationExceptionAsync(validationException, httpContext, cancellationToken);

            return true;
        }

        if (exception.InnerException is ValidationException validationException2)
        {
            await HandleValidationExceptionAsync(validationException2, httpContext, cancellationToken);

            return true;
        }

        if (exception is DataVerificationException ex)
        {
            var apiResult = new ResultModel<string>(string.Empty, true, ex.Message);

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            await httpContext.Response
                    .WriteAsJsonAsync(apiResult, cancellationToken);

            return true;
        }

        if (exception is BadHttpRequestException badHttpRequestException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var apiResult = new ResultModel<string>(string.Empty, true, badHttpRequestException.Message);

            await httpContext.Response
                    .WriteAsJsonAsync(apiResult, cancellationToken);

            return true;
        }

        var data = new ProblemDetails
        {
            Detail = exception.Message,
            Status = StatusCodes.Status500InternalServerError,
            Title = "Something went wrong"
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response
                .WriteAsJsonAsync(data, cancellationToken);

        return true;
    }

    private async ValueTask<bool> HandleValidationExceptionAsync(
        ValidationException validationException,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var error = validationException.Errors.FirstOrDefault()?.ErrorMessage ?? validationException.Message;
        var validateResult = new ResultModel<string>(string.Empty, true, error);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response
                .WriteAsJsonAsync(validateResult, cancellationToken);

        return true;
    }
}
