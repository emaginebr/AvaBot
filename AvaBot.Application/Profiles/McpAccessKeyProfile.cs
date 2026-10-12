using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

public class McpAccessKeyProfile : Profile
{
    public McpAccessKeyProfile()
    {
        // O hash nunca sai da camada de dados: McpAccessKeyInfo nao tem campo para ele.
        CreateMap<McpAccessKey, McpAccessKeyInfo>();
    }
}
