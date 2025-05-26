using System;
using System.Threading;
using System.Threading.Tasks;

namespace UMS.Application.Abstractions.Persistence
{
    /// <summary>
    /// Defines the contract for a Unit of Work.
    /// A Unit of Work tracks changes to entities during a business transaction
    /// and coordinates the writing out of all changes as a single atomic operation.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Saves all changes made in this unit of work to the underlying database.
        /// </summary>
        /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous save operation.
        /// The task result contains the number of state entries written to the database.
        /// </returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // Optionally, you can expose repositories through the Unit of Work,
        // though injecting them directly into handlers is also common.
        // Example: IUserRepository Users { get; }
    }
}
