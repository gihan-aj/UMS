using FluentValidation;

namespace UMS.Application.Features.Clients.Commands.UpdateClient
{
    public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
    {
        public UpdateClientCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ClientName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.RedirectUris).NotNull();
        }
    }
}
