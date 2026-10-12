using AvaBot.Domain.Enums;

namespace AvaBot.Application.Services;

/// <summary>Le o filtro de situacao dos logs de consulta (success | error | timeout) usado pelo painel e pelo MCP.</summary>
public static class PowerBIQueryStatusParser
{
    public const string InvalidMessage = "Status invalido. Use success, error ou timeout";

    public static bool TryParse(string? value, out PowerBIQueryStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        switch (value.Trim().ToLowerInvariant())
        {
            case "success": status = PowerBIQueryStatus.Success; return true;
            case "error": status = PowerBIQueryStatus.Error; return true;
            case "timeout": status = PowerBIQueryStatus.Timeout; return true;
            default: return false;
        }
    }

    public static PowerBIQueryStatus? ParseOrThrow(string? value)
    {
        if (!TryParse(value, out var status))
            throw new ArgumentException(InvalidMessage);
        return status;
    }
}
