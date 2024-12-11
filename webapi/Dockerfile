FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5120

ENV ASPNETCORE_URLS=http://+:5120

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["Transcoder.WebApi.csproj", "./"]
RUN dotnet restore "Transcoder.WebApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Transcoder.WebApi.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Transcoder.WebApi.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Transcoder.WebApi.dll"]
