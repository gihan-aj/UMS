using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Services;

namespace UMS.Infrastructure.Services
{
    public class ReferenceCodeGeneratorService : IReferenceCodeGeneratorService
    {
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
    }
}
