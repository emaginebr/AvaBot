using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class UserUpdateInfoValidator : AbstractValidator<UserUpdateInfo>
{
    public UserUpdateInfoValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length >= 2).WithMessage("Nome deve ter ao menos 2 caracteres")
            .MaximumLength(260).WithMessage("Nome deve ter no maximo 260 caracteres");
    }
}
