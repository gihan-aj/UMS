using FluentValidation;
using FluentValidation.Results;
using Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UMS.SharedKernel;

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
            var error = new Error(
                "Validation.Failure",
                failures.FirstOrDefault()?.ErrorMessage ?? "Validation failed.",
                ErrorType.Validation);

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
            {
                Type valueType = typeof(TResult).GetGenericArguments()[0];

                // Correctly find the generic Result.Failure<TValue>(Error) method
                var genericFailureMethodDefinition = typeof(Result)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.Name == nameof(Result.Failure) &&
                        m.IsGenericMethodDefinition && // Ensure it IS the generic one
                        m.GetGenericArguments().Length == 1 && // Expects one generic type argument
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(Error)
                    );

                if (genericFailureMethodDefinition != null)
                {
                    var concreteFailureMethod = genericFailureMethodDefinition.MakeGenericMethod(valueType);
                    return (TResult)concreteFailureMethod.Invoke(null, new object[] { error })!;
                }
            }
            else if (typeof(TResult) == typeof(Result))
            {
                // More specific retrieval for the non-generic Result.Failure(Error)
                var failureMethod = typeof(Result)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.Name == nameof(Result.Failure) &&
                        !m.IsGenericMethodDefinition && // Ensure it's not the generic one
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(Error) &&
                        m.ReturnType == typeof(Result) // Match return type
                    );

                if (failureMethod != null)
                {
                    return (TResult)failureMethod.Invoke(null, new object[] { error })!;
                }
            }

            throw new InvalidOperationException($"Could not create validation failure result for type {typeof(TResult).Name}. Error: {error.Code} - {error.Message}");
        }
    }
}
