using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
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
            if(error.Type == ErrorType.Validation && error.ValidationErrors.Any())
            {
                // Convert our ValidationErrorDetail list to the dictionary format expected by ValidationProblem
                var validationErrorDictionary = error.ValidationErrors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return Results.ValidationProblem(
                    errors: validationErrorDictionary,
                    detail: error.Message, // The overall message like "One or more validation errors occured."
                    title: "Validation Error", // Consistent title
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // Fallback for other error types or validation errors without specific details
            var errorResponse = new
            {
                title = GetTitleForErrorType(error.Type),
                status = GetStatusCodeForErrorType(error.Type),
                detail = error.Message,
                code = error.Code,
                errors = error.ValidationErrors.Any() ? error.ValidationErrors : null, // Optionally include raw validation errors
            };

            return error.Type switch
            {
                // Validation already handled
                ErrorType.NotFound => Results.NotFound(errorResponse),
                ErrorType.Conflict => Results.Conflict(errorResponse),
                ErrorType.Unauthorized => Results.Problem(
                                        detail: error.Message,
                                        statusCode: StatusCodes.Status401Unauthorized,
                                        title: "Unauthorized",
                                        extensions: new Dictionary<string, object?> { { "errorCode", error.Code } }
                                    ),
                ErrorType.Forbidden => Results.Problem(
                                        detail: error.Message,
                                        statusCode: StatusCodes.Status403Forbidden,
                                        title: "Forbidden",
                                        extensions: new Dictionary<string, object?> { { "errorCode", error.Code } }
                                    ),
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
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
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
            ErrorType.Forbidden => "Forbidden Action",
            ErrorType.Failure => "Internal Server Error",
            _ => "An Error Occurred",
        };
    }
}
