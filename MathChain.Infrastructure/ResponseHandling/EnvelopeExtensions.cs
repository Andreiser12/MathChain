
namespace MathChain.Infrastructure.ResponseHandling
{
    public static class EnvelopeExtensions
    {
        public static Envelope<T> Success<T>(T data, string message = "", int statusCode = 200)
        {
            return new()
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static Envelope<object> Success(string message = "", int statusCode = 200)
        {
            return new()
            {
                Success = true,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static Envelope<T> Failure<T>(string message = "", int statusCode = 400)
        {
            return new()
            {
                Success = false,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static Envelope<object> Failure(string message = "", int statusCode = 400)
        {
            return new()
            {
                Success = false,
                Message = message,
                StatusCode = statusCode
            };
        }


    }
}
