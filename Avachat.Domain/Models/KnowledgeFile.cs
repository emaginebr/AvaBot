using Avachat.Domain.Enums;

namespace Avachat.Domain.Models;

public class KnowledgeFile
{
    public long KnowledgeFileId { get; set; }
    public long? AgentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Processing;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agent? Agent { get; set; }
}
