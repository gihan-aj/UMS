using System;
using System.Security.Cryptography;
using UMS.Domain.Primitives;

namespace UMS.Domain.Users
{
    public class RefreshToken : Entity<Guid>
    {
        public string Token { get; private set; } = null!;

        public DateTime ExpiresAtUtc { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? RevokedAtUtc { get; private set; }

        public Guid UserId { get; private set; } // Foreign key to User

        public string DeviceId { get; private set; } = null!; // Unique identifier for the device

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

        public bool IsRevoked => RevokedAtUtc != null;

        public bool IsActive => !IsRevoked && !IsExpired;

        private RefreshToken(Guid id, Guid userId, string deviceId, string token, DateTime expiresAtUtc) : base(id)
        {
            UserId = userId;
            DeviceId = deviceId;
            Token = token;
            ExpiresAtUtc = expiresAtUtc;
        }

        // Required for EF Core
        private RefreshToken() { }

        public static RefreshToken Create(User user, string deviceId, TimeSpan validity)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("Device ID cannot be null or empty.", nameof(deviceId));
            }

            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var refreshTokenString = Convert.ToBase64String(tokenBytes);
            var expiresAtUtc = DateTime.UtcNow.Add(validity);

            return new RefreshToken(Guid.NewGuid(), user.Id, deviceId, refreshTokenString, expiresAtUtc);
        }

        public void Revoke()
        {
            if (IsActive)
            {
                RevokedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
