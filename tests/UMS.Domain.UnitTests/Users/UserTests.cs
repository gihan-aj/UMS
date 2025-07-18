using FluentAssertions;
using System;
using System.Linq;
using UMS.Domain.Users;
using UMS.Domain.Users.Events;

namespace UMS.Domain.UnitTests.Users
{
    public class UserTests
    {
        // Helper values for creating a user in tests
        private const string TestUserCode = "USR-250716-00001";
        private const string TestEmail = "test@example.com";
        private const string TestPasswordHash = "hashed_password";
        private const string TestFirstName = "Test";
        private const string TestLastName = "User";
        private const int TestActivationTokenExpiryHours = 1;

        [Fact]
        public void Create_Should_InitializeUserCorrectly()
        {
            // --- Act ---
            var user = User.RegisterNew(
                TestUserCode,
                TestEmail,
                TestPasswordHash,
                TestFirstName,
                TestLastName,
                TestActivationTokenExpiryHours,
                null);

            // --- Assert ---
            user.Should().NotBeNull();
            user.Id.Should().NotBeEmpty();
            user.UserCode.Should().Be(TestUserCode);
            user.Email.Should().Be(TestEmail.ToLowerInvariant());
            user.FirstName.Should().Be(TestFirstName);
            user.LastName.Should().Be(TestLastName);
            user.IsActive.Should().BeFalse(); // New users should be inactive
            user.IsDeleted.Should().BeFalse();
            user.ActivationToken.Should().NotBeNullOrEmpty(); // An activation token should be generated
            user.ActivationTokenExpiryUtc.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public void CreateNew_Should_RaiseUserCreatedDomainEvent()
        {
            // --- Act ---
            var user = User.RegisterNew(
                TestUserCode,
                TestEmail,
                TestPasswordHash,
                TestFirstName,
                TestLastName,
                TestActivationTokenExpiryHours,
                null);

            // --- Assert ---
            // Check that the correct domain event was raised
            var domainEvent = user.GetDomainEvents().FirstOrDefault();
            domainEvent.Should().NotBeNull();
            domainEvent.Should().BeOfType<UserRegisteredDomainEvent>();

            var userCreatedEvent = (UserRegisteredDomainEvent)domainEvent!;
            userCreatedEvent.UserId.Should().Be(user.Id);
            userCreatedEvent.Email.Should().Be(user.Email);
            userCreatedEvent.ActivationToken.Should().Be(user.ActivationToken);
        }

        [Fact]
        public void Activate_Should_SetActiveFlagToTrue_And_ClearActivationToken()
        {
            // --- Arrange ---
            var user = User.RegisterNew(TestUserCode, TestEmail, TestPasswordHash, null, null,1, null);
            user.ClearDomainEvents(); // Clear initial event for a clean test

            // --- Act ---
            user.Activate(null);

            // --- Assert ---
            user.IsActive.Should().BeTrue();
            user.ActivationToken.Should().BeNull();
            user.ActivationTokenExpiryUtc.Should().BeNull();
        }

        [Fact]
        public void Activate_Should_RaiseUserAccountActivatedDomainEvent()
        {
            // --- Arrange ---
            var user = User.RegisterNew(TestUserCode, TestEmail, TestPasswordHash, null, null,1, null);
            user.ClearDomainEvents();

            // --- Act ---
            user.Activate(null);

            // --- Assert ---
            var domainEvent = user.GetDomainEvents().FirstOrDefault();
            domainEvent.Should().NotBeNull();
            domainEvent.Should().BeOfType<UserAccountActivatedDomainEvent>();
            ((UserAccountActivatedDomainEvent)domainEvent!).UserId.Should().Be(user.Id);
        }

        [Fact]
        public void AddRefreshToken_Should_AddTokenToCollection()
        {
            // --- Arrange ---
            var user = User.RegisterNew(TestUserCode, TestEmail, TestPasswordHash, null, null,1, null);
            var deviceId = "device-123";
            var validity = TimeSpan.FromDays(7);

            // --- Act ---
            var refreshToken = user.AddRefreshToken(deviceId, validity);

            // --- Assert ---
            user.RefreshTokens.Should().HaveCount(1);
            user.RefreshTokens.First().Should().Be(refreshToken);
            refreshToken.DeviceId.Should().Be(deviceId);
            refreshToken.IsActive.Should().BeTrue();
        }
    }
}
