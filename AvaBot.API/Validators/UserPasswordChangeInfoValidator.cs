using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class UserPasswordChangeInfoValidator : AbstractValidator<UserPasswordChangeInfo>
{
    public UserPasswordChangeInfoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Senha atual e obrigatoria");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha deve ter ao menos 8 caracteres")
            .MinimumLength(8).WithMessage("Nova senha deve ter ao menos 8 caracteres")
            .MaximumLength(128).WithMessage("Nova senha deve ter no maximo 128 caracteres");
    }
}
