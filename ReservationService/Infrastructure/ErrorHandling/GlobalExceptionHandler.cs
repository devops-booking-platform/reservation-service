using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Common.Exceptions;

namespace ReservationService.Infrastructure.ErrorHandling
{
    internal sealed class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
            CancellationToken cancellationToken)
        {
            var statusCode = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Type = exception.GetType().Name,
                Detail = exception.Message,
                Status = statusCode,
                Instance = httpContext.Request.Path
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}