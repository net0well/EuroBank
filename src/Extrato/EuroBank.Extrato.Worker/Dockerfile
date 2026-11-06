# Build a partir da raiz do repositório:
#   docker build -f src/Extrato/EuroBank.Extrato.Worker/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Web/EuroBank.Shared.Web.csproj src/Shared/EuroBank.Shared.Web/
COPY src/Extrato/EuroBank.Extrato.Domain/EuroBank.Extrato.Domain.csproj src/Extrato/EuroBank.Extrato.Domain/
COPY src/Extrato/EuroBank.Extrato.Application/EuroBank.Extrato.Application.csproj src/Extrato/EuroBank.Extrato.Application/
COPY src/Extrato/EuroBank.Extrato.Infrastructure/EuroBank.Extrato.Infrastructure.csproj src/Extrato/EuroBank.Extrato.Infrastructure/
COPY src/Extrato/EuroBank.Extrato.Worker/EuroBank.Extrato.Worker.csproj src/Extrato/EuroBank.Extrato.Worker/
RUN dotnet restore src/Extrato/EuroBank.Extrato.Worker/EuroBank.Extrato.Worker.csproj

COPY src/Shared/ src/Shared/
COPY src/Extrato/ src/Extrato/
RUN dotnet publish src/Extrato/EuroBank.Extrato.Worker/EuroBank.Extrato.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl só para o healthcheck do docker compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EuroBank.Extrato.Worker.dll"]
