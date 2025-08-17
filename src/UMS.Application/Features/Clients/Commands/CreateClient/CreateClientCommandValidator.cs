using FluentValidation;

namespace UMS.Application.Features.Clients.Commands.CreateClient
{
    public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
    {
        public CreateClientCommandValidator()

        {
            RuleFor(x => x.ClientId).NotEmpty().MaximumLength(40);
            RuleFor(x => x.ClientName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.RedirectUris).NotNull();
        }
    }
}
