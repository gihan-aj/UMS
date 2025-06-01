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
        where TResponse : Result // Ensure the response is a Result or Result<T>
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

            var validationResults = await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

            //var validationFailures = _validators
            //    .Select(validator => validator.Validate(context))
            //    .SelectMany(validationResult => validationResult.Errors)
            //    .Where(failure => failure != null)
            //    .ToList();

            var validationFailures = validationResults
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
            var validationErrorDetails = failures
                .Select(f => new ValidationErrorDetail(f.PropertyName, f.ErrorMessage))
                .ToList()
                .AsReadOnly(); // Convert to ReadOnylCollection for the Error record

            // Create a specific validation error object
            var validationErrorObject = Error.Validation(
                code: "General.Validation", // A general code for validation errors
                overallMessage: "One or more validation errors occured.", // An overall message
                errors: validationErrorDetails
            );

            // Use reflection to call the static Result.Failure<TValue>(Error error) or Result.Failure(Error error)
            if(typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
            {
                Type valueType = typeof(TResult).GetGenericArguments()[0];
                var genericFailureMethodDefinition = typeof(Result)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.Name == nameof(Result.Failure) &&
                        m.IsGenericMethodDefinition &&
                        m.GetGenericArguments().Length == 1 &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(Error));

                if(genericFailureMethodDefinition != null)
                {
                    var concreteFailureMethod = genericFailureMethodDefinition.MakeGenericMethod(valueType);
                    return (TResult)concreteFailureMethod.Invoke(null, new object[] { validationErrorObject })!;
                }
            }
            else if (typeof(TResult) == typeof(Result))
            {
                var failureMethod = typeof(Result)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.Name == nameof(Result.Failure) &&
                        !m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(Error) &&
                        m.ReturnType == typeof(Result));

                if(failureMethod != null)
                {
                    return (TResult)failureMethod.Invoke(null, new object[] { validationErrorObject })!;
                }
            }

            // Fallback or throw if we couldn't create the result via reflection
            // This should ideally not be reached if TResponse is always Result or Result<T>
            throw new InvalidOperationException($"Could not create validation failure result fir type {typeof(TResult).FullName}");
        }
    }
}
