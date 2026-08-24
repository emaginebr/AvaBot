namespace AvaBot.Infra.Interfaces.AppServices;

public interface IWppConnectService
{
    Task<string> GenerateTokenAsync(string session);
    Task StartSessionAsync(string session, string webhookUrl);
    Task<string> GetQrCodeAsync(string session);
    Task<string> GetStatusAsync(string session);
    Task CloseSessionAsync(string session);
    Task LogoutSessionAsync(string session);
    Task SendMessageAsync(string session, string phone, string message);
    Task<string?> GetBotLidAsync(string session, string? groupId = null);
}
