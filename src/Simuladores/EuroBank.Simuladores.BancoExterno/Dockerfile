# Build a partir da raiz do repositório:
#   docker build -f src/Simuladores/EuroBank.Simuladores.BancoExterno/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Web/EuroBank.Shared.Web.csproj src/Shared/EuroBank.Shared.Web/
COPY src/Simuladores/EuroBank.Simuladores.Comum/EuroBank.Simuladores.Comum.csproj src/Simuladores/EuroBank.Simuladores.Comum/
COPY src/Simuladores/EuroBank.Simuladores.BancoExterno/EuroBank.Simuladores.BancoExterno.csproj src/Simuladores/EuroBank.Simuladores.BancoExterno/
RUN dotnet restore src/Simuladores/EuroBank.Simuladores.BancoExterno/EuroBank.Simuladores.BancoExterno.csproj

COPY src/Shared/ src/Shared/
COPY src/Simuladores/ src/Simuladores/
RUN dotnet publish src/Simuladores/EuroBank.Simuladores.BancoExterno/EuroBank.Simuladores.BancoExterno.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl só para o healthcheck do docker compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EuroBank.Simuladores.BancoExterno.dll"]
