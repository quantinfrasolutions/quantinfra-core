FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS publish
WORKDIR /src

COPY . .

RUN dotnet restore \
    apps/TradingEngine/TradingEngine.csproj

RUN dotnet publish --no-restore \
    apps/TradingEngine/TradingEngine.csproj \
    -c Release \
    -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS final
LABEL org.opencontainers.image.source=https://github.com/quantinfrasolutions/quantinfra-core

WORKDIR /app
COPY --from=publish /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TradingEngine.dll"]

#MUST BE RUN FROM THE SOLUTION ROOT!