using MathChain.Infrastructure.ResponseHandling;
using Microsoft.AspNetCore.Mvc;

namespace MathChain.API.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult Success<T>(T data, string message = "", int statusCode = StatusCodes.Status200OK)
        {
            return StatusCode(statusCode, EnvelopeExtensions.Success(data, message, statusCode));
        }

        protected IActionResult SuccessMessage(string message, int statusCode = StatusCodes.Status200OK)
        {
            return StatusCode(statusCode, EnvelopeExtensions.Success(message, statusCode));
        }
    }
}
