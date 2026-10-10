using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class UserLoginInfoValidator : AbstractValidator<UserLoginInfo>
{
    public UserLoginInfoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail e obrigatorio");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha e obrigatoria");
    }
}
