using AutoMapper;
using Avachat.Domain.Models;
using Avachat.DTO;

namespace Avachat.Application.Profiles;

public class ChatSessionProfile : Profile
{
    public ChatSessionProfile()
    {
        CreateMap<ChatSession, ChatSessionInfo>()
            .ForMember(d => d.MessageCount, opt => opt.Ignore());
    }
}
