using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Services;
using UMS.Infrastructure.Persistence;
using UMS.Infrastructure.Persistence.Entities;

namespace UMS.Infrastructure.Services
{
    public class ReferenceCodeGeneratorService : IReferenceCodeGeneratorService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ReferenceCodeGeneratorService> _logger;
        private const int SequencePaddingDigits = 5;
        private readonly AsyncRetryPolicy _retryPolicy;

        public ReferenceCodeGeneratorService(ApplicationDbContext dbContext, ILogger<ReferenceCodeGeneratorService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            // Define a retry policy for potential concurrency conflicts during sequence update
            _retryPolicy = Policy
                .Handle<DbUpdateConcurrencyException>() // Retry on concurrency exceptions
                .Or<DbUpdateException>(ex => ex.InnerException is SqlException sqlEx &&
                                                (sqlEx.Number == 2627 || sqlEx.Number == 2601)) // PK violation (could happen if two try to insert new date row)
                .WaitAndRetryAsync(
                    retryCount: 3, // Retry up to 3 times
                    sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)),// Exponential backoff
                    onRetry: (exception, timeSpan, attempt, context) =>
                    {
                        _logger.LogWarning(exception, "Concurrency conflict or PK violation while updating sequence. Retrying attempt {Attempt} in {TimeSpan}...", attempt, timeSpan);
                    });
        }

        public async Task<string> GenerateReferenceCodeAsync(string entityTypePrefix)
        {
            if (string.IsNullOrWhiteSpace(entityTypePrefix) || entityTypePrefix.Length > 4)
            {
                throw new ArgumentException("Entity type prefix must be 1-4 characters long.", nameof(entityTypePrefix));
            }

            var prefixUpper = entityTypePrefix.ToUpperInvariant();
            DateTime currentDate = DateTime.UtcNow.Date; // Use .Date to ensure we are only comparing the date part
            string datePart = currentDate.ToString("yyMMdd", CultureInfo.InvariantCulture);
            int nextSequenceValue = 0;

            // Execute the sequence update within the retry policy
            await _retryPolicy.ExecuteAsync(async () =>
            {
                // For true atomicity and high concurrency, a stored procedure or more advanced DB features
                // (like `SEQUENCE` objects with `NEXT VALUE FOR`) would be more robust.
                // This EF Core approach attempts to handle it with transactions and retries.

                // Start a transaction for this operation to ensure atomicity of read and update/insert.
                // Note: DbContext might already be in a transaction if managed by a UnitOfWork higher up.
                // If so, this might try to enlist or start a nested one depending on provider.
                // For simplicity, let's assume we manage a local transaction here for this specific operation.
                // However, it's better if the UnitOfWork from the command handler spans this.
                // For now, we'll rely on SaveChangesAsync atomicity for a single operation.
                var sequenceRecord = await _dbContext.ReferenceCodeSequences
                    .FirstOrDefaultAsync(s => s.EntityTypePrefix == prefixUpper && s.SequenceDate == currentDate);

                if (sequenceRecord == null)
                {
                    // First code for this prefix on this day
                    nextSequenceValue = 1;
                    _dbContext.ReferenceCodeSequences.Add(new ReferenceCodeSequence
                    {
                        EntityTypePrefix = prefixUpper,
                        SequenceDate = currentDate,
                        LastValue = nextSequenceValue
                    });
                }
                else
                {
                    // Increment existing sequence
                    sequenceRecord.LastValue++;
                    nextSequenceValue = sequenceRecord.LastValue;
                    _dbContext.ReferenceCodeSequences.Update(sequenceRecord);
                }

                // SaveChangesAsync will attempt to commit the transaction.
                // If a DbUpdateConcurrencyException occurs (e.g., another request updated the same row),
                // the retry policy will catch it.
                await _dbContext.SaveChangesAsync();
            });

            if (nextSequenceValue == 0) // Should not happen if retry policy succeeds or throws
            {
                _logger.LogError("Failed to generate sequence number for {Prefix} on {Date} after retries.", prefixUpper, currentDate);
                throw new InvalidOperationException($"Failed to generate sequence number for {prefixUpper} on {currentDate:yyyy-MM-dd}.");
            }

            string formattedSequence = nextSequenceValue.ToString($"D{SequencePaddingDigits}", CultureInfo.InvariantCulture);
            string referenceCode = $"{prefixUpper}-{datePart}-{formattedSequence}";

            _logger.LogInformation("Generated reference code: {ReferenceCode}", referenceCode);
            return referenceCode;
        }
        /*
// In-memory store for daily sequences. Key: "PREFIX-YYMMDD", Value: last sequence number
// This is NOT suitable for production in a distributed environment or across app restarts.
private static readonly ConcurrentDictionary<string, int> _dailySequences = new ConcurrentDictionary<string, int>();
private const int SequencePaddingDigits = 5;

public Task<string> GenerateReferenceCodeAsync(string entityTypePrefix)
{
   if (string.IsNullOrWhiteSpace(entityTypePrefix) || entityTypePrefix.Length > 3)
   {
       throw new ArgumentException("Entity type prefix must be 1-3 characters long.", nameof(entityTypePrefix));
   }

   // Use current UTC date to ensure consistency
   DateTime currentDate = DateTime.UtcNow;
   // Using "yyMMdd" format as agreed. For Sri Lanka, local time might be preferred by users,
   // but UTC is safer for backend sequence generation to avoid timezone issues.
   // Let's stick to UTC for the sequence key.
   string datePart = currentDate.ToString("yyMMdd", CultureInfo.InvariantCulture);

   string sequenceKey = $"{entityTypePrefix.ToUpperInvariant()}-{datePart}";

   // Automatically increment the sequence for the given key (prefix + date)
   // AddOrUpdate ensures thread safety for this in-memory counter.
   int nextSequenceValue = _dailySequences.AddOrUpdate(
       key: sequenceKey,
       addValueFactory: _ => 1, // If key doesn't exist, start sequence at 1
       updateValueFactory: (_, currentValue) => currentValue + 1 // If key exists, increment
       );

   string formattedSequence = nextSequenceValue.ToString($"D{SequencePaddingDigits}", CultureInfo.InvariantCulture);

   string referenceCode = $"{entityTypePrefix.ToUpperInvariant()}-{datePart}-{formattedSequence}";

   return Task.FromResult(referenceCode);
}
*/
    }
}
