using System.Text.RegularExpressions;
using AvaBot.Application.Services;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Calibration.Local;

/// <summary>
/// Base de conhecimento lida de uma pasta local no lugar do Elasticsearch.
/// Reproduz a busca de producao: mesmos chunks da ingestao (IngestionService.ChunkText) e
/// match textual com BM25 sobre tokens em minusculas, como o analyzer "standard" do indice
/// (sem stemming, sem remover acento). So devolve trechos com ao menos um termo da pergunta.
/// </summary>
public class LocalKnowledgeBase : IElasticsearchService
{
    private const double K1 = 1.2;
    private const double B = 0.75;
    private static readonly Regex TokenPattern = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    private readonly List<Chunk> _chunks = new();
    private readonly Dictionary<string, int> _documentFrequency = new();
    private double _averageLength;

    public string? Folder { get; private set; }
    public int FileCount { get; private set; }
    public int ChunkCount => _chunks.Count;

    public static LocalKnowledgeBase Load(string? folder, IEnumerable<string> extensions, int chunkSize, int overlap)
    {
        var knowledgeBase = new LocalKnowledgeBase { Folder = folder };

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return knowledgeBase;

        var allowed = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => allowed.Contains(Path.GetExtension(f)))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        foreach (var file in files)
        {
            foreach (var text in IngestionService.ChunkText(File.ReadAllText(file), chunkSize, overlap))
                knowledgeBase.Add(Path.GetRelativePath(folder, file), text);
        }

        knowledgeBase.FileCount = files.Count;
        knowledgeBase._averageLength = knowledgeBase._chunks.Count == 0 ? 0 : knowledgeBase._chunks.Average(c => c.Length);
        return knowledgeBase;
    }

    private void Add(string file, string text)
    {
        var terms = Tokenize(text).GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
        _chunks.Add(new Chunk(file, text, terms, terms.Values.Sum()));

        foreach (var term in terms.Keys)
            _documentFrequency[term] = _documentFrequency.GetValueOrDefault(term) + 1;
    }

    public Task<List<string>> TextSearchAsync(long agentId, string queryText, int topK = 5)
    {
        var queryTerms = Tokenize(queryText).Distinct().ToList();
        var total = _chunks.Count;

        var results = _chunks
            .Select(chunk => (chunk, score: queryTerms.Sum(term => Score(chunk, term, total))))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => x.chunk.Text)
            .ToList();

        return Task.FromResult(results);
    }

    private double Score(Chunk chunk, string term, int total)
    {
        if (!chunk.Terms.TryGetValue(term, out var frequency))
            return 0;

        var documentFrequency = _documentFrequency[term];
        var idf = Math.Log(1 + (total - documentFrequency + 0.5) / (documentFrequency + 0.5));
        var norm = frequency + K1 * (1 - B + B * chunk.Length / Math.Max(_averageLength, 1));

        return idf * frequency * (K1 + 1) / norm;
    }

    private static IEnumerable<string> Tokenize(string text) =>
        TokenPattern.Matches(text).Select(m => m.Value.ToLowerInvariant());

    // Ingestao nao existe na calibracao: a pasta e a fonte.
    public Task CreateIndexAsync() => Task.CompletedTask;
    public Task IndexChunksAsync(long agentId, long knowledgeFileId, List<ChunkData> chunks) => throw LocalAgentRepository.ReadOnly();
    public Task DeleteChunksByFileIdAsync(long knowledgeFileId) => throw LocalAgentRepository.ReadOnly();
    public Task DeleteChunksByAgentIdAsync(long agentId) => throw LocalAgentRepository.ReadOnly();

    private sealed record Chunk(string File, string Text, Dictionary<string, int> Terms, int Length);
}
