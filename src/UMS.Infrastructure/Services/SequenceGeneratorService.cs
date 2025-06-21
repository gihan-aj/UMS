using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Services;
using UMS.Infrastructure.Persistence;

namespace UMS.Infrastructure.Services
{
    public class SequenceGeneratorService : ISequenceGeneratorService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SequenceGeneratorService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public SequenceGeneratorService(
            ApplicationDbContext dbContext, 
            ILogger<SequenceGeneratorService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _retryPolicy = Policy
                .Handle<DbUpdateConcurrencyException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)));
        }

        public async Task<TId> GetNextIdAsync<TId>(string sequenceName, CancellationToken cancellationToken = default) 
            where TId : struct, IComparable, IConvertible
        {
            long nextValue = 0;

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var sequence = await _dbContext.NumericSequences.FirstOrDefaultAsync(s => s.SequenceName == sequenceName, cancellationToken);
                if(sequence is null)
                {
                    sequence = new Persistence.Entities.NumericSequence { SequenceName = sequenceName, LastValue = 1 };
                    await _dbContext.NumericSequences.AddAsync(sequence, cancellationToken);
                    nextValue = 1;
                }
                else
                {
                    sequence.LastValue++;
                    nextValue = sequence.LastValue;
                    _dbContext.NumericSequences.Update(sequence);
                }

                //await _dbContext.SaveChangesAsync(cancellationToken);
            });

            if(nextValue == 0)
            {
                throw new InvalidOperationException($"Failed to generate sequence for '{sequenceName}'.");
            }

            // Check if the value fits into the target type TId
            try
            {
                var convertedValue = (TId)Convert.ChangeType(nextValue, typeof(TId));
                return convertedValue;
            }
            catch (OverflowException ex)
            {
                _logger.LogError(ex, "Sequence value {NextValue} for '{SequenceName}' overflows target type {TypeName}.", nextValue, sequenceName, typeof(TId).Name);
                throw new OverflowException($"Sequence value for '{sequenceName}' has exceeded the maximum value for type {typeof(TId).Name}.", ex);
            }
        }
    }
}
