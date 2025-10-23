FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MatchBy/MatchBy.Client/MatchBy.Client.csproj", "MatchBy/MatchBy.Client/"]
RUN dotnet restore "MatchBy/MatchBy.Client/MatchBy.Client.csproj"
COPY . .
WORKDIR "/src/MatchBy/MatchBy.Client"
RUN dotnet build "./MatchBy.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MatchBy.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MatchBy.Client.dll"]
