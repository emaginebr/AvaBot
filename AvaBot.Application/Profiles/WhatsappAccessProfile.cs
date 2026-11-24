using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

public class WhatsappAccessProfile : Profile
{
    public WhatsappAccessProfile()
    {
        CreateMap<WhatsappContact, WhatsappContactInfo>()
            .ForMember(d => d.Source, o => o.MapFrom(s => (int)s.Source));

        CreateMap<WhatsappAccessRequest, WhatsappAccessRequestInfo>()
            .ForMember(d => d.Phone, o => o.MapFrom(s => s.PhoneNormalized))
            .ForMember(d => d.Status, o => o.MapFrom(s => (int)s.Status));
    }
}
