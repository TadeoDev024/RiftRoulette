FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RiftRoulette.csproj", "."]
RUN dotnet restore "./RiftRoulette.csproj"
COPY . .
RUN dotnet build "RiftRoulette.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RiftRoulette.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RiftRoulette.dll"]