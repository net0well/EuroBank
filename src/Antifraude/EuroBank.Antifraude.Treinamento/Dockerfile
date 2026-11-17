# Build a partir da raiz do repositório:
#   docker build -f src/Antifraude/EuroBank.Antifraude.Treinamento/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache de camadas.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/EuroBank.Shared/EuroBank.Shared.csproj src/Shared/EuroBank.Shared/
COPY src/Shared/EuroBank.Shared.Persistence/EuroBank.Shared.Persistence.csproj src/Shared/EuroBank.Shared.Persistence/
COPY src/Shared/EuroBank.Shared.Messaging/EuroBank.Shared.Messaging.csproj src/Shared/EuroBank.Shared.Messaging/
COPY src/Antifraude/EuroBank.Antifraude.Features/EuroBank.Antifraude.Features.csproj src/Antifraude/EuroBank.Antifraude.Features/
COPY src/Antifraude/EuroBank.Antifraude.Domain/EuroBank.Antifraude.Domain.csproj src/Antifraude/EuroBank.Antifraude.Domain/
COPY src/Antifraude/EuroBank.Antifraude.Application/EuroBank.Antifraude.Application.csproj src/Antifraude/EuroBank.Antifraude.Application/
COPY src/Antifraude/EuroBank.Antifraude.Infrastructure/EuroBank.Antifraude.Infrastructure.csproj src/Antifraude/EuroBank.Antifraude.Infrastructure/
COPY src/Antifraude/EuroBank.Antifraude.Treinamento/EuroBank.Antifraude.Treinamento.csproj src/Antifraude/EuroBank.Antifraude.Treinamento/
RUN dotnet restore src/Antifraude/EuroBank.Antifraude.Treinamento/EuroBank.Antifraude.Treinamento.csproj

COPY src/Shared/ src/Shared/
COPY src/Antifraude/ src/Antifraude/
RUN dotnet publish src/Antifraude/EuroBank.Antifraude.Treinamento/EuroBank.Antifraude.Treinamento.csproj -c Release -o /app --no-restore

# Console, sem ASP.NET: basta o runtime. O CSV sintético fica em /app/dados (um volume no compose).
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app .
RUN mkdir dados && chown $APP_UID dados
USER $APP_UID
ENTRYPOINT ["dotnet", "EuroBank.Antifraude.Treinamento.dll"]
CMD ["treinar"]
