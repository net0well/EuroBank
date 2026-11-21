# Build a partir da raiz do repositório:
#   docker build -f src/Antifraude/EuroBank.Antifraude.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
# global.json fixa a versão mínima do SDK: uma imagem de SDK antiga em cache falha aqui, em vez de publicar sem
# os assets que as versões novas trazem (o blazor.web.js, por exemplo).
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Web/EuroBank.Shared.Web.csproj src/Shared/EuroBank.Shared.Web/
COPY src/Shared/EuroBank.Shared.Persistence/EuroBank.Shared.Persistence.csproj src/Shared/EuroBank.Shared.Persistence/
COPY src/Shared/EuroBank.Shared.Messaging/EuroBank.Shared.Messaging.csproj src/Shared/EuroBank.Shared.Messaging/
COPY src/Antifraude/EuroBank.Antifraude.Features/EuroBank.Antifraude.Features.csproj src/Antifraude/EuroBank.Antifraude.Features/
COPY src/Antifraude/EuroBank.Antifraude.Domain/EuroBank.Antifraude.Domain.csproj src/Antifraude/EuroBank.Antifraude.Domain/
COPY src/Antifraude/EuroBank.Antifraude.Application/EuroBank.Antifraude.Application.csproj src/Antifraude/EuroBank.Antifraude.Application/
COPY src/Antifraude/EuroBank.Antifraude.Infrastructure/EuroBank.Antifraude.Infrastructure.csproj src/Antifraude/EuroBank.Antifraude.Infrastructure/
COPY src/Antifraude/EuroBank.Antifraude.Api/EuroBank.Antifraude.Api.csproj src/Antifraude/EuroBank.Antifraude.Api/
RUN dotnet restore src/Antifraude/EuroBank.Antifraude.Api/EuroBank.Antifraude.Api.csproj

COPY src/Shared/ src/Shared/
COPY src/Antifraude/ src/Antifraude/
RUN dotnet publish src/Antifraude/EuroBank.Antifraude.Api/EuroBank.Antifraude.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# grpc_health_probe: o healthcheck do compose usa o protocolo grpc.health.v1 na porta gRPC.
ARG TARGETARCH
ARG GRPC_HEALTH_PROBE_VERSION=v0.4.63
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && curl -fsSL -o /usr/local/bin/grpc_health_probe \
       "https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/${GRPC_HEALTH_PROBE_VERSION}/grpc_health_probe-linux-${TARGETARCH:-amd64}" \
    && chmod +x /usr/local/bin/grpc_health_probe
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
# 8080: REST do analista, JSON transcoding, Swagger e /health (HTTP/1.1). 8081: gRPC (HTTP/2 sem TLS).
EXPOSE 8080 8081
ENTRYPOINT ["dotnet", "EuroBank.Antifraude.Api.dll"]
