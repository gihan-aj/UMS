using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Common.Messaging.Commands;
using UMS.Domain.Users;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;

        public RegisterUserCommandHandler(IPasswordHasherService passwordHasherService, IUserRepository userRepository)
        {
            _passwordHasherService = passwordHasherService ?? throw new ArgumentNullException(nameof(userRepository)); ;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(passwordHasherService));
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            // 1. Check if user already exists
            bool userExists = await _userRepository.ExistsByEmailAsync(command.Email.ToLowerInvariant());
            if (userExists)
            {
                // User with this email already exists. Return a conflict error.
                return Result.Failure<Guid>(new Error(
                    "User.AlreadyExists",
                    $"A user with the email '{command.Email}' already exists.",
                    ErrorType.Conflict));
            }

            // 2. Hash the password
            // The command.Password should be the plain text password.
            string passwordHash = _passwordHasherService.HashPassword(command.Password);

            // 3. Create the User domain entity
            // The User.Create factory method can enforce initial domain invariants.
            User newUser;
            try
            {
                newUser = User.Create(
                    email: command.Email, // Let the User entity handle ToLowerInvariant if desired
                    passwordHash: passwordHash,
                    firstName: command.FirstName,
                    lastName: command.LastName
                );
            }
            catch (ArgumentException ex) // Catch potential exceptions from User.Create
            {
                // This could happen if User.Create has its own validation (e.g., on email format if not handled by FluentValidation)
                return Result.Failure<Guid>(new Error("User.CreationError", ex.Message, ErrorType.Validation));
            }


            // 4. Persist the user
            try
            {
                await _userRepository.AddAsync(newUser);
                // Assuming AddAsync throws for critical persistence failures or is void.
                // If AddAsync returned a Result, we would check that here.
            }
            catch (Exception ex) // Catch potential exceptions from the data store
            {
                // Log the exception ex
                return Result.Failure<Guid>(new Error(
                    "User.PersistenceError",
                    "An error occurred while saving the user.",
                    ErrorType.Failure)); // Or a more specific infrastructure error
            }

            // 5. TODO: Publish domain event (e.g., UserCreatedNotification using IPublisher)
            // var userCreatedNotification = new UserCreatedNotification(newUser.Id, newUser.Email);
            // await _publisher.Publish(userCreatedNotification, cancellationToken);

            // 6. Return success with the new User's ID
            return Result<Guid>.Success(newUser.Id);
        }
    }
}
