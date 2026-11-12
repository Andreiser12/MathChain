using MathChain.Domain.Exceptions;
using MathChain.Infrastructure.ResponseHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MathChain.Infrastructure.ExceptionHandling
{
    public class GlobalExceptionHandler
    {
        private const string GenericErrorMessage = "An unexpected error occurred.";

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception) when (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                ForbiddenException => StatusCodes.Status403Forbidden,
                BusinessRuleException => StatusCodes.Status400BadRequest,
                DomainException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            string message;

            if (exception is DomainException)
            {
                message = exception.Message;
                _logger.LogWarning("{ExceptionType} on {Method} {Path}: {Message}",
                    exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);
            }
            else
            {
                message = GenericErrorMessage;
                _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(EnvelopeExtensions.Failure<object>(message, statusCode));
        }
    }
}
