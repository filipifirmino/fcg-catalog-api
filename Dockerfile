FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/FCG-CATALOG-API.Api/FCG-CATALOG-API.Api.csproj", "src/FCG-CATALOG-API.Api/"]
COPY ["src/FCG-CATALOG-API.Application/FCG-CATALOG-API.Application.csproj", "src/FCG-CATALOG-API.Application/"]
COPY ["src/FCG-CATALOG-API.Domain/FCG-CATALOG-API.Domain.csproj", "src/FCG-CATALOG-API.Domain/"]
COPY ["src/FCG-CATALOG-API.Infra/FCG-CATALOG-API.Infra.csproj", "src/FCG-CATALOG-API.Infra/"]

RUN dotnet restore "src/FCG-CATALOG-API.Api/FCG-CATALOG-API.Api.csproj"

COPY . .
RUN dotnet publish "src/FCG-CATALOG-API.Api/FCG-CATALOG-API.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

COPY --chown=catalogApiUser:catalogApiUser --from=build /app/publish .
USER catalogApiUser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "FCG-CATALOG-API.Api.dll"]
