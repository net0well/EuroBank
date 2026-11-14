# Build a partir da raiz do repositório:
#   docker build -f src/Notificacoes/EuroBank.Notificacoes.Worker/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Web/EuroBank.Shared.Web.csproj src/Shared/EuroBank.Shared.Web/
COPY src/Shared/EuroBank.Shared.Messaging/EuroBank.Shared.Messaging.csproj src/Shared/EuroBank.Shared.Messaging/
COPY src/Notificacoes/EuroBank.Notificacoes.Worker/EuroBank.Notificacoes.Worker.csproj src/Notificacoes/EuroBank.Notificacoes.Worker/
RUN dotnet restore src/Notificacoes/EuroBank.Notificacoes.Worker/EuroBank.Notificacoes.Worker.csproj

COPY src/Shared/ src/Shared/
COPY src/Notificacoes/ src/Notificacoes/
RUN dotnet publish src/Notificacoes/EuroBank.Notificacoes.Worker/EuroBank.Notificacoes.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl só para o healthcheck do docker compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EuroBank.Notificacoes.Worker.dll"]
