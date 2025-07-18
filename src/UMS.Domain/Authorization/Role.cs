﻿using System;
using System.Collections.Generic;
using UMS.Domain.Authorization.Events;
using UMS.Domain.Primitives;

namespace UMS.Domain.Authorization
{
    /// <summary>
    /// Represents a role that can be assigned to users, grouping a set of permissions.
    /// This is an Aggregate Root.
    /// </summary>
    public class Role : AggregateRoot<byte>, ISoftDeletable
    {
        public string Name { get; private set; } = string.Empty;

        // Navigation to the join table for permission
        public ICollection<RolePermission> Permissions { get; private set; } = new HashSet<RolePermission>();

        // --- ISoftDeletable Implementation ---
        public bool IsDeleted { get; private set; }

        public DateTime? DeletedAtUtc { get; private set; }

        public Guid? DeletedBy { get; private set; }

        private Role() { }

        public static Role Create(byte id, string name, Guid? createdByUserId)
        {
            var newRole = new Role { Id = id, Name = name };
            newRole.SetCreationAudit(createdByUserId);

            return newRole;
        }

        public void UpdateName(string newName, Guid? modifiedByUserId)
        {
            if (string.IsNullOrWhiteSpace(newName) || newName.Equals(Name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            Name = newName;
            SetModificationAudit(modifiedByUserId);
        }


        // --- Domain Methods ---
        public void MarkAsDeleted(Guid? deletedByUserId) 
        {
            if (IsDeleted) return;

            IsDeleted = true;
            DeletedBy = deletedByUserId;
            DeletedAtUtc = DateTime.UtcNow;
            SetModificationAudit(deletedByUserId);

            RaiseDomainEvent(new RoleSoftDeletedDomainEvent(this.Id));
        }
    }
}
