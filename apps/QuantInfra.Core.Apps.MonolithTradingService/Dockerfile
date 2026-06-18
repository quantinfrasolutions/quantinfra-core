FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS projects-env
WORKDIR /src
COPY ./StrategyExecutor/*.sln .
COPY ./StrategyExecutor/**/*.csproj ./
COPY ./StrategyExecutor/**/**/*.csproj ./
COPY ./StrategyExecutor/**/**/**/*.csproj ./
RUN dotnet sln StrategyExecutor.sln list | grep ".csproj" \
    | while read -r line; do \ 
    mkdir -p $(dirname $line); \
    mv $(basename $line) $(dirname $line); \
    done;

FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS restore
WORKDIR /src
COPY --from=projects-env /src /src
RUN dotnet restore Executables/v6/AccountService/

FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
WORKDIR /src
COPY --from=restore /src /src
COPY --from=restore /root/.local/share/NuGet /root/.local/share/NuGet
COPY --from=restore /root/.nuget/packages /root/.nuget/packages
COPY --from=restore /tmp/NuGetScratchroot /tmp/NuGetScratchroot
COPY StrategyExecutor/. . 

FROM build AS publish
RUN dotnet publish Executables/v6/AccountService -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS final
LABEL org.opencontainers.image.source=https://github.com/quantgem/strategyconnector
RUN apt-get update
RUN apt-get install less
WORKDIR /app
COPY --from=publish /app .
EXPOSE 8080
EXPOSE 6100
EXPOSE 6101
EXPOSE 6200
ENTRYPOINT ["dotnet", "AccountService.dll"]

#MUST BE RUN FROM THE SOLUTION ROOT!