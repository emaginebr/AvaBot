using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

public class AgentProfile : Profile
{
    public AgentProfile()
    {
        CreateMap<Agent, AgentInfo>();

        CreateMap<Agent, AgentChatConfigInfo>();

        CreateMap<AgentInsertInfo, Agent>()
            .ForMember(d => d.AgentId, opt => opt.Ignore())
            .ForMember(d => d.Slug, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.TelegramWebhookSecret, opt => opt.Ignore())
            .ForMember(d => d.WhatsappToken, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.KnowledgeFiles, opt => opt.Ignore())
            .ForMember(d => d.ChatSessions, opt => opt.Ignore());
    }
}
