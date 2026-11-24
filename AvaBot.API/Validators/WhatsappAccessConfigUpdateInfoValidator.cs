using FluentValidation;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class WhatsappAccessConfigUpdateInfoValidator : AbstractValidator<WhatsappAccessConfigUpdateInfo>
{
    public const string EmptyMessage = "A frase nao pode ficar vazia";
    public const string TooLongMessage = "A frase deve ter no maximo 500 caracteres";

    public WhatsappAccessConfigUpdateInfoValidator()
    {
        RuleFor(x => x.Mode)
            .Must(m => Enum.IsDefined(typeof(WhatsappAccessMode), m))
            .WithMessage("Modo de acesso invalido");

        // null = restaurar o padrao; texto informado precisa ter conteudo.
        RuleFor(x => x.OfferMessage).Custom((v, ctx) => ValidateMessage(v, ctx, "oferta"));
        RuleFor(x => x.ConfirmationMessage).Custom((v, ctx) => ValidateMessage(v, ctx, "confirmacao"));
        RuleFor(x => x.WaitingMessage).Custom((v, ctx) => ValidateMessage(v, ctx, "aguardando aprovacao"));
        RuleFor(x => x.DeclinedMessage).Custom((v, ctx) => ValidateMessage(v, ctx, "recusa"));
    }

    private static void ValidateMessage(string? value, ValidationContext<WhatsappAccessConfigUpdateInfo> ctx, string label)
    {
        if (value == null)
            return;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            ctx.AddFailure($"{EmptyMessage} ({label})");
        else if (trimmed.Length > WhatsappAccessDefaults.MaxMessageLength)
            ctx.AddFailure($"{TooLongMessage} ({label})");
    }
}
