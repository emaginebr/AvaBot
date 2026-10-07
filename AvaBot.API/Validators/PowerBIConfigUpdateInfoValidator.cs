using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class PowerBIConfigUpdateInfoValidator : AbstractValidator<PowerBIConfigUpdateInfo>
{
    public PowerBIConfigUpdateInfoValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID e obrigatorio")
            .Must(BeGuid).WithMessage("Tenant ID deve ser um GUID valido");

        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID e obrigatorio")
            .Must(BeGuid).WithMessage("Client ID deve ser um GUID valido");

        RuleFor(x => x.ClientSecret)
            .MaximumLength(500).WithMessage("Client Secret deve ter no maximo 500 caracteres");
    }

    private static bool BeGuid(string? value) => Guid.TryParse(value, out _);
}
