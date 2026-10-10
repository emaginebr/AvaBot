namespace AvaBot.Domain.Models;

public class User
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Status { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();

    public bool IsActive => Status == 1;

    // O e-mail e a identidade da conta: comparado sem caixa e sem espacos nas pontas (FR-002).
    public static string NormalizeEmail(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
