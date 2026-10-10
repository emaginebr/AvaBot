using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Application.Profiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // O hash nunca sai da camada de dados: UserInfo nao tem campo para ele.
        CreateMap<User, UserInfo>();
    }
}
