using FluentValidation;
using Avachat.Domain.DTOs;

namespace Avachat.API.Validators;

public class AgentInsertInfoValidator : AbstractValidator<AgentInsertInfo>
{
    public AgentInsertInfoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome e obrigatorio")
            .MaximumLength(260).WithMessage("Nome deve ter no maximo 260 caracteres");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug e obrigatorio")
            .MaximumLength(260).WithMessage("Slug deve ter no maximo 260 caracteres")
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$").WithMessage("Slug deve conter apenas letras minusculas, numeros e hifens");

        RuleFor(x => x.SystemPrompt)
            .NotEmpty().WithMessage("Prompt de sistema e obrigatorio");
    }
}
