FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Avachat.API/Avachat.API.csproj", "Avachat.API/"]
COPY ["Avachat.Application/Avachat.Application.csproj", "Avachat.Application/"]
COPY ["Avachat.Domain/Avachat.Domain.csproj", "Avachat.Domain/"]
COPY ["Avachat.DTO/Avachat.DTO.csproj", "Avachat.DTO/"]
COPY ["Avachat.Infra/Avachat.Infra.csproj", "Avachat.Infra/"]
COPY ["Avachat.Infra.Interfaces/Avachat.Infra.Interfaces.csproj", "Avachat.Infra.Interfaces/"]
RUN dotnet restore "Avachat.API/Avachat.API.csproj"
COPY . .
WORKDIR "/src/Avachat.API"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Avachat.API.dll"]
