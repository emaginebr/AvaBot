using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

public class AgentProfile : Profile
{
    public AgentProfile()
    {
        // A credencial e derivada: so o indicador de presenca sai para o cliente.
        CreateMap<Agent, AgentInfo>()
            .ForMember(d => d.HasOpenAIApiKey, opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.OpenAIApiKeyEncrypted)));

        CreateMap<Agent, AgentChatConfigInfo>();

        CreateMap<AgentInsertInfo, Agent>()
            .ForMember(d => d.AgentId, opt => opt.Ignore())
            .ForMember(d => d.Slug, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.TelegramWebhookSecret, opt => opt.Ignore())
            .ForMember(d => d.WhatsappToken, opt => opt.Ignore())
            .ForMember(d => d.OpenAIApiKeyEncrypted, opt => opt.Ignore())
            .ForMember(d => d.PowerBIEnabled, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.KnowledgeFiles, opt => opt.Ignore())
            .ForMember(d => d.ChatSessions, opt => opt.Ignore())
            .ForMember(d => d.PowerBIDatasets, opt => opt.Ignore())
            .ForMember(d => d.PowerBIConfig, opt => opt.Ignore());
    }
}
