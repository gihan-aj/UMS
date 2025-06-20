using System;
using System.Threading;
using System.Threading.Tasks;

namespace UMS.Application.Abstractions.Services
{
    public interface ISequenceGeneratorService
    {
        Task<TId> GetNextIdAsync<TId>(string sequenceName, CancellationToken cancellationToken = default) 
            where TId : struct, IComparable, IConvertible;
    }
}
