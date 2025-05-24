using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;

namespace UMS.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
         where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _logger.LogInformation("----- Handling request {RequestName} -----", typeof(TRequest).Name);
            _logger.LogDebug("Request data: {@Request}", request); // Requires Serilog or similar for structured logging of @Request

            var response = await next(); // Call the next delegate in the chain

            _logger.LogInformation("----- Handled {RequestName} - Response: {ResponseName} -----", typeof(TRequest).Name, typeof(TResponse).Name);
            _logger.LogDebug("Response data: {@Response}", response);

            return response;
        }
    }
}
