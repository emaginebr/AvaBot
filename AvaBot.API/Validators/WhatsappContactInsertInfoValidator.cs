using FluentValidation;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class WhatsappContactInsertInfoValidator : AbstractValidator<WhatsappContactInsertInfo>
{
    public const int MaxNameLength = 120;

    public WhatsappContactInsertInfoValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Nome e obrigatorio")
            .Must(n => (n ?? string.Empty).Trim().Length <= MaxNameLength).WithMessage("Nome deve ter no maximo 120 caracteres");

        RuleFor(x => x.Phone)
            .Must(p => !string.IsNullOrWhiteSpace(p)).WithMessage("Telefone e obrigatorio");

        RuleFor(x => x.Phone)
            .Must(p => WhatsappPhone.Normalize(p) != null)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Telefone invalido. Informe DDD e numero, com ou sem codigo do pais");
    }
}
