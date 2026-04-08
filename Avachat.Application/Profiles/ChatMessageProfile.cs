using AutoMapper;
using Avachat.Domain.Models;
using Avachat.DTO;

namespace Avachat.Application.Profiles;

public class ChatMessageProfile : Profile
{
    public ChatMessageProfile()
    {
        CreateMap<ChatMessage, ChatMessageInfo>()
            .ForMember(d => d.SenderType, opt => opt.MapFrom(s => (int)s.SenderType));
    }
}
