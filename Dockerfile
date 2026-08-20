FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# libfontconfig1 e exigido pela lib nativa do SkiaSharp (usada para decodificar QR Codes
# nos logs de debug do WhatsApp) -- ausente na imagem base minima do aspnet.
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AvaBot.API/AvaBot.API.csproj", "AvaBot.API/"]
COPY ["AvaBot.Application/AvaBot.Application.csproj", "AvaBot.Application/"]
COPY ["AvaBot.Domain/AvaBot.Domain.csproj", "AvaBot.Domain/"]
COPY ["AvaBot.DTO/AvaBot.DTO.csproj", "AvaBot.DTO/"]
COPY ["AvaBot.Infra/AvaBot.Infra.csproj", "AvaBot.Infra/"]
COPY ["AvaBot.Infra.Interfaces/AvaBot.Infra.Interfaces.csproj", "AvaBot.Infra.Interfaces/"]
RUN dotnet restore "AvaBot.API/AvaBot.API.csproj"
COPY . .
WORKDIR "/src/AvaBot.API"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AvaBot.API.dll"]
