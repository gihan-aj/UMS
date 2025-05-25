using System;

namespace UMS.Domain.Primitives
{
    /// <summary>
    /// Interface for entities that support soft deletion.
    /// </summary>
    public interface ISoftDeletable
    {
        bool IsDeleted { get; }

        DateTime? DeletedAtUtc { get; }

        void MarkAsDeleted(Guid? deletedByUserId); // Method to perform soft delete
    }
}
