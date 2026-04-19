FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["dpp.opentakrouter/dpp.opentakrouter.csproj", "dpp.opentakrouter/"]
RUN dotnet restore "dpp.opentakrouter/dpp.opentakrouter.csproj"
COPY . .
WORKDIR "/src/dpp.opentakrouter"
RUN dotnet build "dpp.opentakrouter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "dpp.opentakrouter.csproj" -c Release -o /app/publish -r linux-x64 --no-self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "opentakrouter.dll"]
