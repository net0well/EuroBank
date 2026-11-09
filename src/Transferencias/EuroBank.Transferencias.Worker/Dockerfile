# Build a partir da raiz do repositório:
#   docker build -f src/Transferencias/EuroBank.Transferencias.Worker/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Web/EuroBank.Shared.Web.csproj src/Shared/EuroBank.Shared.Web/
COPY src/Shared/EuroBank.Shared.Persistence/EuroBank.Shared.Persistence.csproj src/Shared/EuroBank.Shared.Persistence/
COPY src/Shared/EuroBank.Shared.Messaging/EuroBank.Shared.Messaging.csproj src/Shared/EuroBank.Shared.Messaging/
COPY src/Transferencias/EuroBank.Transferencias.Domain/EuroBank.Transferencias.Domain.csproj src/Transferencias/EuroBank.Transferencias.Domain/
COPY src/Transferencias/EuroBank.Transferencias.Application/EuroBank.Transferencias.Application.csproj src/Transferencias/EuroBank.Transferencias.Application/
COPY src/Transferencias/EuroBank.Transferencias.Infrastructure/EuroBank.Transferencias.Infrastructure.csproj src/Transferencias/EuroBank.Transferencias.Infrastructure/
COPY src/Transferencias/EuroBank.Transferencias.Worker/EuroBank.Transferencias.Worker.csproj src/Transferencias/EuroBank.Transferencias.Worker/
RUN dotnet restore src/Transferencias/EuroBank.Transferencias.Worker/EuroBank.Transferencias.Worker.csproj

COPY src/Shared/ src/Shared/
COPY src/Transferencias/ src/Transferencias/
RUN dotnet publish src/Transferencias/EuroBank.Transferencias.Worker/EuroBank.Transferencias.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl só para o healthcheck do docker compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EuroBank.Transferencias.Worker.dll"]
