using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class WhatsappAccessRequestApproveInfoValidator : AbstractValidator<WhatsappAccessRequestApproveInfo>
{
    public WhatsappAccessRequestApproveInfoValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => n == null || n.Trim().Length <= WhatsappContactInsertInfoValidator.MaxNameLength)
            .WithMessage("Nome deve ter no maximo 120 caracteres");
    }
}
