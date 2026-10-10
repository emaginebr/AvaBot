namespace AvaBot.Calibration;

public class CommandLine
{
    public const string Usage =
        """
        Uso: dotnet run --project AvaBot.Calibration -- [opções]

          -q, --question "texto"   pergunta isolada
          -f, --file caminho       arquivo JSON de conversas
          -o, --output caminho     arquivo .md do relatório (padrão: diretório temporário)
          -c, --config caminho     appsettings (padrão: appsettings.json ao lado do executável)
              --refresh-schema     regera o schema no Power BI, mantendo as descrições do arquivo
              --list-datasets      lista workspaces e datasets que o aplicativo enxerga no Power BI
          -h, --help               esta ajuda

        Sem --question/--file, usa Calibration:Question ou Calibration:File do appsettings.json.
        O relatório sai no console (stdout); progresso e caminho do arquivo saem no stderr.
        """;

    public string? Question { get; private set; }
    public string? FilePath { get; private set; }
    public string? OutputPath { get; private set; }
    public string? ConfigPath { get; private set; }
    public bool RefreshSchema { get; private set; }
    public bool ListDatasets { get; private set; }
    public bool Help { get; private set; }

    public static CommandLine Parse(string[] args)
    {
        var command = new CommandLine();

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length
                ? args[++i]
                : throw new ArgumentException($"Falta o valor de {args[i]}.");

            switch (args[i])
            {
                case "-q" or "--question": command.Question = Next(); break;
                case "-f" or "--file": command.FilePath = Next(); break;
                case "-o" or "--output": command.OutputPath = Next(); break;
                case "-c" or "--config": command.ConfigPath = Next(); break;
                case "--refresh-schema": command.RefreshSchema = true; break;
                case "--list-datasets": command.ListDatasets = true; break;
                case "-h" or "--help": command.Help = true; break;
                default: throw new ArgumentException($"Opção desconhecida: {args[i]}");
            }
        }

        if (command.Question != null && command.FilePath != null)
            throw new ArgumentException("Use --question ou --file, não os dois.");

        return command;
    }
}

public static class Paths
{
    /// <summary>
    /// Caminho relativo: tenta o diretorio atual e, se nao existir, a raiz do repositorio
    /// (onde esta AvaBot.sln), para funcionar com dotnet run de qualquer pasta.
    /// </summary>
    public static string Resolve(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        var fromCurrent = Path.GetFullPath(path);
        if (File.Exists(fromCurrent) || Directory.Exists(fromCurrent))
            return fromCurrent;

        var root = FindRepositoryRoot();
        return root != null ? Path.GetFullPath(Path.Combine(root, path)) : fromCurrent;
    }

    private static string? FindRepositoryRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "AvaBot.sln")))
                    return dir.FullName;
            }
        }

        return null;
    }
}
