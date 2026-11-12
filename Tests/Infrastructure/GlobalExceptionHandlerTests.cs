using MathChain.Domain.Exceptions;
using MathChain.Infrastructure.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace MathChain.Tests.Infrastructure
{
    public class GlobalExceptionHandlerTests
    {
        private static DefaultHttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<string> ReadBodyAsync(HttpContext context)
        {
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body);
            return await reader.ReadToEndAsync();
        }

        private static Task InvokeAsync(HttpContext context, Exception? exception)
        {
            var handler = new GlobalExceptionHandler(
                _ => exception is null ? Task.CompletedTask : throw exception,
                NullLogger<GlobalExceptionHandler>.Instance);

            return handler.InvokeAsync(context);
        }

        public static TheoryData<Exception, int> DomainExceptions => new()
        {
            { new NotFoundException("Classroom not found."), StatusCodes.Status404NotFound },
            { new UnauthorizedException("Invalid wallet signature."), StatusCodes.Status401Unauthorized },
            { new ForbiddenException("Only the teacher can delete this classroom."), StatusCodes.Status403Forbidden },
            { new BusinessRuleException("Student is already enrolled in this classroom."), StatusCodes.Status400BadRequest }
        };

        [Theory]
        [MemberData(nameof(DomainExceptions))]
        public async Task InvokeAsync_DomainException_WritesEnvelopeWithStatusCode(Exception exception, int expectedStatusCode)
        {
            var context = CreateContext();

            await InvokeAsync(context, exception);

            var expectedBody = $"{{\"success\":false,\"data\":null,\"message\":\"{exception.Message}\",\"statusCode\":{expectedStatusCode},\"errors\":[]}}";
            Assert.Equal(expectedStatusCode, context.Response.StatusCode);
            Assert.StartsWith("application/json", context.Response.ContentType);
            Assert.Equal(expectedBody, await ReadBodyAsync(context));
        }

        [Fact]
        public async Task InvokeAsync_UnexpectedException_HidesDetailsAndReturns500()
        {
            var context = CreateContext();

            await InvokeAsync(context, new InvalidOperationException("Sequence contains no elements"));

            var body = await ReadBodyAsync(context);
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Contains("\"message\":\"An unexpected error occurred.\"", body);
            Assert.DoesNotContain("Sequence contains no elements", body);
        }

        [Fact]
        public async Task InvokeAsync_NoException_LeavesResponseUntouched()
        {
            var context = CreateContext();

            await InvokeAsync(context, null);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal(string.Empty, await ReadBodyAsync(context));
        }

        [Fact]
        public async Task InvokeAsync_ResponseAlreadyStarted_RethrowsException()
        {
            var context = CreateContext();
            context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

            await Assert.ThrowsAsync<NotFoundException>(() => InvokeAsync(context, new NotFoundException("Classroom not found.")));
        }

        private sealed class StartedResponseFeature : HttpResponseFeature
        {
            public override bool HasStarted => true;
        }
    }
}
