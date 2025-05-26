using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;

namespace UMS.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core implementation of the IUnitOfWork interface.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;

        // If you choose to expose repositories via UoW:
        // public IUserRepository Users { get; }

        public UnitOfWork(ApplicationDbContext dbContext /*, IUserRepository userRepository (if exposing) */)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            // Users = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        /// <summary>
        /// Saves all changes made in the DbContext to the database.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Here we could also dispatch domain events before saving changes,
            // or handle other cross-cutting concerns related to saving.
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            // Dispose the DbContext if the UnitOfWork is responsible for its lifetime.
            // However, since DbContext is typically managed by DI (Scoped),
            // explicit disposal here might not be necessary or could even be problematic
            // if other services in the same scope still need it.
            // For now, let DI handle DbContext disposal.
            // _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
