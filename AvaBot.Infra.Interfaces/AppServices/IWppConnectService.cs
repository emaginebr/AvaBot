namespace AvaBot.Infra.Interfaces.AppServices;

public interface IWppConnectService
{
    Task<string> GenerateTokenAsync(string session);
    Task StartSessionAsync(string session, string webhookUrl);
    Task<string> GetQrCodeAsync(string session);
    Task<string> GetStatusAsync(string session);
    /// <summary>
    /// Estado da sessao no WPP Connect (status-session): status e, enquanto aguarda o pareamento,
    /// o QR Code atual em data URL. O WhatsApp troca o QR a cada ~20s e o WPP Connect acompanha,
    /// entao consultar este metodo periodicamente sempre devolve o QR valido no momento.
    /// </summary>
    Task<WppSessionState> GetSessionStateAsync(string session);
    Task CloseSessionAsync(string session);
    Task LogoutSessionAsync(string session);
    Task SendMessageAsync(string session, string phone, string message);
    Task<string?> GetBotLidAsync(string session, string? groupId = null);
    /// <summary>Telefone (so digitos) de um LID do WhatsApp; null quando nao for possivel resolver.</summary>
    Task<string?> GetPhoneByLidAsync(string session, string lid);
}

/// <summary>Status de uma sessao do WPP Connect e o QR Code atual (null fora do pareamento).</summary>
public record WppSessionState(string Status, string? QrCode);
