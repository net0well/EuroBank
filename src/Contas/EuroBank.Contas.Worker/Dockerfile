# Build a partir da raiz do repositório:
#   docker build -f src/Contas/EuroBank.Contas.Worker/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Web/EuroBank.Shared.Web.csproj src/Shared/EuroBank.Shared.Web/
COPY src/Shared/EuroBank.Shared.Persistence/EuroBank.Shared.Persistence.csproj src/Shared/EuroBank.Shared.Persistence/
COPY src/Shared/EuroBank.Shared.Messaging/EuroBank.Shared.Messaging.csproj src/Shared/EuroBank.Shared.Messaging/
COPY src/Contas/EuroBank.Contas.Domain/EuroBank.Contas.Domain.csproj src/Contas/EuroBank.Contas.Domain/
COPY src/Contas/EuroBank.Contas.Application/EuroBank.Contas.Application.csproj src/Contas/EuroBank.Contas.Application/
COPY src/Contas/EuroBank.Contas.Infrastructure/EuroBank.Contas.Infrastructure.csproj src/Contas/EuroBank.Contas.Infrastructure/
COPY src/Contas/EuroBank.Contas.Worker/EuroBank.Contas.Worker.csproj src/Contas/EuroBank.Contas.Worker/
RUN dotnet restore src/Contas/EuroBank.Contas.Worker/EuroBank.Contas.Worker.csproj

COPY src/Shared/ src/Shared/
COPY src/Contas/ src/Contas/
RUN dotnet publish src/Contas/EuroBank.Contas.Worker/EuroBank.Contas.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl só para o healthcheck do docker compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EuroBank.Contas.Worker.dll"]
