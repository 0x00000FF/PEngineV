FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY Sources/PEngineV.slnx .
COPY Sources/PEngineV/PEngineV.csproj PEngineV/
COPY Sources/PEngineV.Test/PEngineV.Test.csproj PEngineV.Test/
RUN dotnet restore PEngineV.slnx
COPY Sources/ .
RUN dotnet build PEngineV/PEngineV.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish PEngineV/PEngineV.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PEngineV.dll"]
