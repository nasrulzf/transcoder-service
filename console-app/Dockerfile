FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["TranscoderService.Console.csproj", "./"]
RUN dotnet restore "TranscoderService.Console.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TranscoderService.Console.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TranscoderService.Console.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# RUN chmod +x third-party/packager-linux-x64

ENTRYPOINT ["dotnet", "TranscoderService.Console.dll"]
