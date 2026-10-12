using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class McpAccessKeyInsertInfoValidator : AbstractValidator<McpAccessKeyInsertInfo>
{
    public McpAccessKeyInsertInfoValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Nome e obrigatorio")
            .Must(n => (n ?? string.Empty).Trim().Length <= 100).WithMessage("Nome deve ter no maximo 100 caracteres");
    }
}
