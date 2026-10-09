using AvaBot.Calibration;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Tests.Calibration;

public class DatasetCatalogTest
{
    private static List<PowerBIWorkspace> Workspaces() => new()
    {
        new()
        {
            Id = "ws-1", Name = "ABIPESCA",
            Datasets = new()
            {
                new() { Id = "ds-1", Name = "ABIPESCA - Comércio Internacional" },
                new() { Id = "ds-2", Name = "Produção" }
            }
        },
        new()
        {
            Id = "ws-2", Name = "Outro",
            Datasets = new() { new() { Id = "ds-3", Name = "Produção" } }
        }
    };

    [Fact]
    public void Resolve_ShouldUseEveryAccessibleDataset_WhenNoneIsConfigured()
    {
        var resolved = DatasetCatalog.Resolve(new PowerBIOptions(), Workspaces());

        Assert.Equal(new[] { "ds-1", "ds-2", "ds-3" }, resolved.Select(d => d.DatasetId));
        Assert.Equal("abipesca_comercio_internacional", resolved[0].ToolKey);
        Assert.Equal(new[] { "producao", "producao_2" }, resolved.Skip(1).Select(d => d.ToolKey));
    }

    [Fact]
    public void Resolve_ShouldFilterByWorkspaceNameOrId()
    {
        var options = new PowerBIOptions { Workspaces = new() { "abipesca" } };

        var resolved = DatasetCatalog.Resolve(options, Workspaces());

        Assert.Equal(new[] { "ds-1", "ds-2" }, resolved.Select(d => d.DatasetId));
        Assert.All(resolved, d => Assert.Equal("ws-1", d.WorkspaceId));
    }

    [Fact]
    public void Resolve_ShouldFindIdsByName_AndKeepConfiguredFields()
    {
        var options = new PowerBIOptions
        {
            Datasets = new() { new() { Name = "abipesca - comércio internacional", Description = "Comtrade", ToolKey = "abipesca" } }
        };

        var dataset = Assert.Single(DatasetCatalog.Resolve(options, Workspaces()));

        Assert.Equal(("ws-1", "ds-1"), (dataset.WorkspaceId, dataset.DatasetId));
        Assert.Equal("abipesca", dataset.ToolKey);
        Assert.Equal("Comtrade", dataset.Description);
    }

    [Fact]
    public void Resolve_ShouldRejectAmbiguousOrMissingNames()
    {
        var ambiguous = new PowerBIOptions { Datasets = new() { new() { Name = "Produção" } } };
        var missing = new PowerBIOptions { Datasets = new() { new() { Name = "Não existe" } } };

        var ex = Assert.Throws<InvalidOperationException>(() => DatasetCatalog.Resolve(ambiguous, Workspaces()));
        Assert.Contains("mais de um workspace", ex.Message);
        Assert.Throws<InvalidOperationException>(() => DatasetCatalog.Resolve(missing, Workspaces()));

        // Com WorkspaceId, o nome deixa de ser ambiguo.
        ambiguous.Datasets[0].WorkspaceId = "ws-2";
        Assert.Equal("ds-3", Assert.Single(DatasetCatalog.Resolve(ambiguous, Workspaces())).DatasetId);
    }

    [Fact]
    public void NeedsDiscovery_ShouldBeFalseOnlyWhenEveryDatasetHasIds()
    {
        Assert.True(DatasetCatalog.NeedsDiscovery(new PowerBIOptions()));
        Assert.True(DatasetCatalog.NeedsDiscovery(new PowerBIOptions { Datasets = new() { new() { Name = "x" } } }));
        Assert.False(DatasetCatalog.NeedsDiscovery(new PowerBIOptions
        {
            Datasets = new() { new() { Name = "x", WorkspaceId = "w", DatasetId = "d" } }
        }));
    }
}
