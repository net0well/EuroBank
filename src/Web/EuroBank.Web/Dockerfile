# Build a partir da raiz do repositório:
#   docker build -f src/Web/EuroBank.Web/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
# global.json fixa a versão mínima do SDK: uma imagem de SDK antiga em cache falha aqui, em vez de publicar sem
# os assets que as versões novas trazem (o blazor.web.js, por exemplo).
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Web/EuroBank.Shared.Web.csproj src/Shared/EuroBank.Shared.Web/
COPY src/Shared/EuroBank.Shared.Messaging/EuroBank.Shared.Messaging.csproj src/Shared/EuroBank.Shared.Messaging/
COPY src/Web/EuroBank.Web/EuroBank.Web.csproj src/Web/EuroBank.Web/
RUN dotnet restore src/Web/EuroBank.Web/EuroBank.Web.csproj

COPY src/Shared/ src/Shared/
COPY src/Web/EuroBank.Web/ src/Web/EuroBank.Web/
RUN dotnet publish src/Web/EuroBank.Web/EuroBank.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl só para o healthcheck do docker compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EuroBank.Web.dll"]
