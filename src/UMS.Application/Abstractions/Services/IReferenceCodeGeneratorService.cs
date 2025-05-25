using System.Threading.Tasks;

namespace UMS.Application.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that generates human-readable reference codes for entities.
    /// </summary>
    public interface IReferenceCodeGeneratorService
    {
        /// <summary>
        /// Generates a new reference code for the specified entity type prefix.
        /// </summary>
        /// <param name="entityTypePrefix">The prefix for the entity type (e.g., "USR", "ORD").</param>
        /// <returns>A unique, human-readable reference code string.</returns>
        Task<string> GenerateReferenceCodeAsync(string entityTypePrefix);
    }
}
