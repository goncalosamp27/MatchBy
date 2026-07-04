FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["MatchBy/MatchBy/MatchBy.csproj", "MatchBy/MatchBy/"]

RUN dotnet restore "MatchBy/MatchBy/MatchBy.csproj"

COPY . .

WORKDIR "/src/MatchBy/MatchBy"

RUN dotnet publish "MatchBy.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:TreatWarningsAsErrors=false \
    /p:CodeAnalysisTreatWarningsAsErrors=false \
    /p:EnforceCodeStyleInBuild=false \
    /p:RunAnalyzersDuringBuild=false

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MatchBy.dll"]