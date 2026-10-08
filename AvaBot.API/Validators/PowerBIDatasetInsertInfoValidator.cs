using FluentValidation;
using AvaBot.DTO;

namespace AvaBot.API.Validators;

public class PowerBIDatasetInsertInfoValidator : AbstractValidator<PowerBIDatasetInsertInfo>
{
    public PowerBIDatasetInsertInfoValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace ID e obrigatorio")
            .Must(BeGuid).WithMessage("Workspace ID deve ser um GUID valido");

        RuleFor(x => x.DatasetId)
            .NotEmpty().WithMessage("Dataset ID e obrigatorio")
            .Must(BeGuid).WithMessage("Dataset ID deve ser um GUID valido");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome e obrigatorio")
            .MaximumLength(120).WithMessage("Nome deve ter no maximo 120 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Descricao deve ter no maximo 1000 caracteres");
    }

    private static bool BeGuid(string? value) => Guid.TryParse(value, out _);
}
