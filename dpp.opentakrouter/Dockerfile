#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["dpp.opentakrouter/dpp.opentakrouter.csproj", "dpp.opentakrouter/"]
RUN dotnet restore "dpp.opentakrouter/dpp.opentakrouter.csproj"
COPY . .
WORKDIR "/src/dpp.opentakrouter"
RUN dotnet build "dpp.opentakrouter.csproj" -c Release -o /app/build --self-contained --runtime linux-64

FROM build AS publish
RUN dotnet publish "dpp.opentakrouter.csproj" -c Release -o /app/publish --self-contained --runtime linux-64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "opentakrouter.dll"]