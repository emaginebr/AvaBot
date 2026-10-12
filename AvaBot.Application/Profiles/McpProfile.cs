using AutoMapper;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

/// <summary>
/// Projecoes do servidor MCP (feature 017): so indicadores de credencial, nunca o valor.
/// </summary>
public class McpProfile : Profile
{
    public McpProfile()
    {
        CreateMap<Agent, AgentMcpSummaryInfo>()
            .ForMember(d => d.Active, opt => opt.MapFrom(s => s.Status == 1))
            .ForMember(d => d.HasOpenAIApiKey, opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.OpenAIApiKeyEncrypted)));

        CreateMap<Agent, AgentMcpInfo>()
            .IncludeBase<Agent, AgentMcpSummaryInfo>()
            .ForMember(d => d.TelegramBotConfigured, opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.TelegramBotToken)))
            .ForMember(d => d.WhatsappConfigured, opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.WhatsappToken)));

        // channel e messageCount sao preenchidos pelo SessionQueryService.
        CreateMap<ChatSession, ChatSessionMcpInfo>()
            .ForMember(d => d.Channel, opt => opt.Ignore())
            .ForMember(d => d.MessageCount, opt => opt.Ignore());

        CreateMap<ChatMessage, ChatMessageMcpInfo>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.SenderType == SenderType.User ? "visitante" : "agente"));
    }
}
