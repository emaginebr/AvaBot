using AvaBot.Application.Services;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Calibration;

/// <summary>
/// Resolve os datasets da calibracao a partir do Power BI (mesma listagem do painel,
/// ListWorkspacesWithDatasetsAsync): sem PowerBI:Datasets usa todos os acessiveis
/// (filtrados por PowerBI:Workspaces); um dataset so com Name tem os IDs descobertos pelo nome.
/// </summary>
public static class DatasetCatalog
{
    public const int MaxToolKeyLength = 60;

    public static bool NeedsDiscovery(PowerBIOptions options) =>
        options.Datasets.Count == 0
        || options.Datasets.Any(d => string.IsNullOrWhiteSpace(d.WorkspaceId) || string.IsNullOrWhiteSpace(d.DatasetId));

    public static List<DatasetOptions> Resolve(PowerBIOptions options, IReadOnlyList<PowerBIWorkspace> workspaces)
    {
        var allowed = workspaces.Where(w => MatchesWorkspaceFilter(options.Workspaces, w)).ToList();
        var resolved = new List<DatasetOptions>();

        if (options.Datasets.Count == 0)
        {
            resolved.AddRange(allowed.SelectMany(w => w.Datasets.Select(d => new DatasetOptions
            {
                Name = d.Name,
                WorkspaceId = w.Id,
                DatasetId = d.Id
            })));

            if (resolved.Count == 0)
                throw new InvalidOperationException(options.Workspaces.Count > 0
                    ? $"Nenhum dataset acessível nos workspaces {string.Join(", ", options.Workspaces)}."
                    : "O aplicativo não enxerga nenhum dataset no Power BI. Verifique as permissões do workspace.");
        }
        else
        {
            foreach (var configured in options.Datasets)
                resolved.Add(ResolveOne(configured, allowed));
        }

        AssignToolKeys(resolved);
        return resolved;
    }

    private static DatasetOptions ResolveOne(DatasetOptions configured, List<PowerBIWorkspace> workspaces)
    {
        if (!string.IsNullOrWhiteSpace(configured.WorkspaceId) && !string.IsNullOrWhiteSpace(configured.DatasetId))
            return configured;

        if (string.IsNullOrWhiteSpace(configured.Name) && string.IsNullOrWhiteSpace(configured.DatasetId))
            throw new InvalidOperationException("Cada item de PowerBI:Datasets precisa de Name ou de WorkspaceId + DatasetId.");

        var candidates = workspaces
            .Where(w => string.IsNullOrWhiteSpace(configured.WorkspaceId) || w.Id.Equals(configured.WorkspaceId, StringComparison.OrdinalIgnoreCase))
            .SelectMany(w => w.Datasets.Select(d => (Workspace: w, Dataset: d)))
            .Where(x => !string.IsNullOrWhiteSpace(configured.DatasetId)
                ? x.Dataset.Id.Equals(configured.DatasetId, StringComparison.OrdinalIgnoreCase)
                : x.Dataset.Name.Equals(configured.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        var label = string.IsNullOrWhiteSpace(configured.Name) ? configured.DatasetId : configured.Name;

        if (candidates.Count == 0)
            throw new InvalidOperationException($"Dataset '{label}' não encontrado nos workspaces acessíveis. Use --list-datasets para ver os disponíveis.");

        if (candidates.Count > 1)
            throw new InvalidOperationException(
                $"Dataset '{label}' existe em mais de um workspace ({string.Join(", ", candidates.Select(c => c.Workspace.Name))}). " +
                "Informe WorkspaceId ou restrinja PowerBI:Workspaces.");

        var (workspace, dataset) = candidates[0];

        return new DatasetOptions
        {
            Name = string.IsNullOrWhiteSpace(configured.Name) ? dataset.Name : configured.Name,
            ToolKey = configured.ToolKey,
            Description = configured.Description,
            WorkspaceId = workspace.Id,
            DatasetId = dataset.Id,
            SchemaFile = configured.SchemaFile
        };
    }

    private static bool MatchesWorkspaceFilter(List<string> filter, PowerBIWorkspace workspace) =>
        filter.Count == 0
        || filter.Any(f => f.Equals(workspace.Id, StringComparison.OrdinalIgnoreCase)
                        || f.Equals(workspace.Name, StringComparison.OrdinalIgnoreCase));

    // Mesma regra do painel (PowerBIService.GenerateToolKeyAsync): slug com "_", ate 60, sufixo _2, _3...
    private static void AssignToolKeys(List<DatasetOptions> datasets)
    {
        var used = new HashSet<string>(datasets.Where(d => !string.IsNullOrWhiteSpace(d.ToolKey)).Select(d => d.ToolKey));

        foreach (var dataset in datasets.Where(d => string.IsNullOrWhiteSpace(d.ToolKey)))
        {
            var baseKey = AgentService.Slugify(dataset.Name).Replace('-', '_').TrimEnd('_');
            if (baseKey.Length > MaxToolKeyLength) baseKey = baseKey[..MaxToolKeyLength];
            if (baseKey.Length == 0) baseKey = "dataset";

            var key = baseKey;
            for (var i = 2; used.Contains(key); i++)
            {
                var suffix = $"_{i}";
                key = (baseKey.Length + suffix.Length > MaxToolKeyLength ? baseKey[..(MaxToolKeyLength - suffix.Length)] : baseKey) + suffix;
            }

            dataset.ToolKey = key;
            used.Add(key);
        }
    }

    public static string Describe(IReadOnlyList<PowerBIWorkspace> workspaces)
    {
        var lines = new List<string>();

        foreach (var workspace in workspaces)
        {
            lines.Add($"{workspace.Name}  (WorkspaceId: {workspace.Id})");
            if (workspace.Datasets.Count == 0)
                lines.Add("    (nenhum dataset)");
            foreach (var dataset in workspace.Datasets)
                lines.Add($"    {dataset.Name}  (DatasetId: {dataset.Id})");
        }

        return lines.Count == 0 ? "Nenhum workspace acessível." : string.Join(Environment.NewLine, lines);
    }
}
