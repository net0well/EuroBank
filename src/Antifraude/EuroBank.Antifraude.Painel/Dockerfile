# Build a partir da raiz do repositório:
#   docker build -f src/Antifraude/EuroBank.Antifraude.Painel/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Web/EuroBank.Shared.Web.csproj src/Shared/EuroBank.Shared.Web/
COPY src/Shared/EuroBank.Shared.Messaging/EuroBank.Shared.Messaging.csproj src/Shared/EuroBank.Shared.Messaging/
COPY src/Antifraude/EuroBank.Antifraude.Painel/EuroBank.Antifraude.Painel.csproj src/Antifraude/EuroBank.Antifraude.Painel/
RUN dotnet restore src/Antifraude/EuroBank.Antifraude.Painel/EuroBank.Antifraude.Painel.csproj

COPY src/Shared/ src/Shared/
COPY src/Antifraude/EuroBank.Antifraude.Painel/ src/Antifraude/EuroBank.Antifraude.Painel/
RUN dotnet publish src/Antifraude/EuroBank.Antifraude.Painel/EuroBank.Antifraude.Painel.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl só para o healthcheck do docker compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EuroBank.Antifraude.Painel.dll"]
