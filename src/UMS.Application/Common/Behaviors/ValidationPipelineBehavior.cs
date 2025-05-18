using FluentValidation;
using FluentValidation.Results;
using Mediator;
using UMS.SharedKernal;

namespace UMS.Application.Common.Behaviors
{
    public class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : Result
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators ?? throw new ArgumentNullException(nameof(validators)); 
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (!_validators.Any())
            {
                // No validators registered for this request type.
                return await next();
            }

            // Create a validation context
            var context = new ValidationContext<TRequest>(request);

            // Run all validators and collect failures
            // Run synchronously for simplicity here, can be made async if validators use async rules.
            var validationFailures = _validators
                .Select(validator => validator.Validate(context))
                .SelectMany(validationResult => validationResult.Errors)
                .Where(failure => failure != null)
                .ToList();

            if (validationFailures.Any())
            {
                // Validation failed, create a failure Result.
                // We need to create the specific TResponse type (Result or Result<T>)
                // with the validation errors.
                return CreateValidationResult<TResponse>(validationFailures);
            }

            // Validation passed, continue to the next step in the pipeline (usually the handler).
            return await next();
        }

        // Helper method to create the correct type of Result (Result or Result<TValue>)
        // This uses reflection, which isn't ideal for performance.
        // A more optimized approach might involve specific interfaces or known types.
        private static TResult CreateValidationResult<TResult>(List<ValidationFailure> failures)
            where TResult : Result
        {
            // Combine validation failures into a single Error object or multiple errors.
            // For simplicity, let's take the first error message for now.
            // A better approach might concatenate messages or return a structured error.
            var error = new Error(
                 "Validation.Failure", // Generic validation code
                 failures.FirstOrDefault()?.ErrorMessage ?? "Validation failed.", // First message
                 ErrorType.Validation);


            // Check if TResult is Result<T> or just Result
            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
            {
                // It's Result<TValue>
                // Get the TValue type
                Type valueType = typeof(TResult).GetGenericArguments()[0];

                // Get the static Failure<TValue> method from the non-generic Result class
                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(valueType);

                if (failureMethod != null)
                {
                    // Invoke Result.Failure<TValue>(error)
                    return (TResult)failureMethod.Invoke(null, new object[] { error })!;
                }
            }
            else if (typeof(TResult) == typeof(Result))
            {
                // It's the non-generic Result
                // Get the static Failure method from the non-generic Result class
                var failureMethod = typeof(Result)
                   .GetMethod(nameof(Result.Failure), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new[] { typeof(Error) }, null);

                if (failureMethod != null)
                {
                    // Invoke Result.Failure(error)
                    return (TResult)failureMethod.Invoke(null, new object[] { error })!;
                }
            }

            // Fallback or throw if we couldn't create the result via reflection
            throw new InvalidOperationException($"Could not create validation failure result for type {typeof(TResult).Name}");
        }
    }
}
