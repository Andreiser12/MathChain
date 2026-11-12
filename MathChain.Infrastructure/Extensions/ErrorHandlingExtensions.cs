using MathChain.Infrastructure.ExceptionHandling;
using MathChain.Infrastructure.ResponseHandling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace MathChain.Infrastructure.Extensions
{
    public static class ErrorHandlingExtensions
    {
        private const string InvalidRequestMessage = "The request is invalid.";
        private const string InvalidValueMessage = "The value is invalid.";

        public static IMvcBuilder AddEnvelopeValidationResponses(this IMvcBuilder builder)
        {
            return builder.ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                        .SelectMany(entry => entry.Value!.Errors.Select(error =>
                        {
                            var message = string.IsNullOrWhiteSpace(error.ErrorMessage) ? InvalidValueMessage : error.ErrorMessage;
                            return string.IsNullOrEmpty(entry.Key) ? message : $"{entry.Key}: {message}";
                        }))
                        .ToList();

                    var envelope = EnvelopeExtensions.Failure<object>(InvalidRequestMessage, StatusCodes.Status400BadRequest);
                    envelope.Errors = errors;

                    return new BadRequestObjectResult(envelope);
                };
            });
        }

        public static IApplicationBuilder UseEnvelopeErrorHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<GlobalExceptionHandler>();

            app.UseStatusCodePages(async statusCodeContext =>
            {
                var response = statusCodeContext.HttpContext.Response;
                var message = ReasonPhrases.GetReasonPhrase(response.StatusCode);

                await response.WriteAsJsonAsync(EnvelopeExtensions.Failure<object>(message, response.StatusCode));
            });

            return app;
        }
    }
}
