using AutoMapper;
using Avachat.Domain.Models;
using Avachat.DTO;

namespace Avachat.Application.Profiles;

public class KnowledgeFileProfile : Profile
{
    public KnowledgeFileProfile()
    {
        CreateMap<KnowledgeFile, KnowledgeFileInfo>()
            .ForMember(d => d.ProcessingStatus, opt => opt.MapFrom(s => (int)s.ProcessingStatus));
    }
}
