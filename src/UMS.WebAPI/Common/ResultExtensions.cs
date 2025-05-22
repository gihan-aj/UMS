using UMS.SharedKernel;

namespace UMS.WebAPI.Common
{
    public static class ResultExtensions
    {
        public static IResult ToHttpResult<TValue>(this Result<TValue> result, Func<TValue, IResult>? onSuccess = null)
        {
            if (result.IsSuccess)
            {
                return onSuccess != null ? onSuccess(result.Value) : Results.Ok(result.Value);
            }

            return MapErrorToHttpResult(result.Error);
        }

        public static IResult ToHttpResult(this Result result, Func<IResult>? onSuccess = null)
        {
            if (result.IsSuccess)
            {
                return onSuccess != null ? onSuccess() : Results.Ok();
            }

            return MapErrorToHttpResult(result.Error);
        }

        private static IResult MapErrorToHttpResult(Error error)
        {
            // Create an error response object. You can customize this.
            var errorResponse = new
            {
                title = GetTitleForErrorType(error.Type),
                status = GetStatusCodeForErrorType(error.Type),
                detail = error.Message,
                code = error.Code,
                // You could add more details, like a list of validation errors if Error.Message was structured
            };

            return error.Type switch
            {
                ErrorType.Validation => Results.ValidationProblem(
                    errors: new Dictionary<string, string[]> { { error.Code ?? "Validation", new[] { error.Message } } }, // Simplified for now
                    detail: error.Message,
                    title: "Validation Error",
                    statusCode: StatusCodes.Status400BadRequest
                ), // Or Results.BadRequest(errorResponse)
                ErrorType.NotFound => Results.NotFound(errorResponse),
                ErrorType.Conflict => Results.Conflict(errorResponse),
                ErrorType.Unauthorized => Results.Unauthorized(), // Or Results.Problem with details
                ErrorType.Failure => Results.Problem(
                                        detail: error.Message,
                                        statusCode: StatusCodes.Status500InternalServerError,
                                        title: "An unexpected error occurred.",
                                        extensions: new Dictionary<string, object?> { { "errorCode", error.Code } }
                                    ),
                ErrorType.None => Results.Problem( // Should not happen for a failed result
                                        detail: "An unexpected error occurred without a specific type.",
                                        statusCode: StatusCodes.Status500InternalServerError,
                                        title: "Internal Server Error"
                                    ),
                _ => Results.Problem( // Default for unhandled error types
                                        detail: error.Message,
                                        statusCode: StatusCodes.Status500InternalServerError,
                                        title: "An unexpected error occurred.",
                                        extensions: new Dictionary<string, object?> { { "errorCode", error.Code } }
                                    )
            };
        }

        private static int GetStatusCodeForErrorType(ErrorType errorType) => errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        private static string GetTitleForErrorType(ErrorType errorType) => errorType switch
        {
            ErrorType.Validation => "Validation Error",
            ErrorType.NotFound => "Resource Not Found",
            ErrorType.Conflict => "Conflict Error",
            ErrorType.Unauthorized => "Unauthorized Access",
            ErrorType.Failure => "Internal Server Error",
            _ => "An Error Occurred",
        };
    }
}
