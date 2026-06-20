FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/FCG-CATALOG-API.Api/FCG-CATALOG-API.Api.csproj", "src/FCG-CATALOG-API.Api/"]
COPY ["src/FCG-CATALOG-API.Application/FCG-CATALOG-API.Application.csproj", "src/FCG-CATALOG-API.Application/"]
COPY ["src/FCG-CATALOG-API.Domain/FCG-CATALOG-API.Domain.csproj", "src/FCG-CATALOG-API.Domain/"]
COPY ["src/FCG-CATALOG-API.Infra/FCG-CATALOG-API.Infra.csproj", "src/FCG-CATALOG-API.Infra/"]
RUN dotnet restore "src/FCG-CATALOG-API.Api/FCG-CATALOG-API.Api.csproj"
COPY . .
RUN dotnet publish "src/FCG-CATALOG-API.Api/FCG-CATALOG-API.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FCG-CATALOG-API.Api.dll"]
