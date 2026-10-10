using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class UserRegisterInfoValidator : AbstractValidator<UserRegisterInfo>
{
    public UserRegisterInfoValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length >= 2).WithMessage("Nome deve ter ao menos 2 caracteres")
            .MaximumLength(260).WithMessage("Nome deve ter no maximo 260 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail e obrigatorio")
            .EmailAddress().WithMessage("E-mail invalido")
            .MaximumLength(260).WithMessage("E-mail deve ter no maximo 260 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha deve ter ao menos 8 caracteres")
            .MinimumLength(8).WithMessage("Senha deve ter ao menos 8 caracteres")
            .MaximumLength(128).WithMessage("Senha deve ter no maximo 128 caracteres");
    }
}
