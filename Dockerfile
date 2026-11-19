# syntax=docker/dockerfile:1

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia apenas os .csproj primeiro: enquanto eles não mudarem, a camada de
# restore do NuGet é reaproveitada do cache do Docker.
COPY MeuEstudo.Api/MeuEstudo.Api.csproj MeuEstudo.Api/
COPY MeuEstudo.Application/MeuEstudo.Application.csproj MeuEstudo.Application/
COPY MeuEstudo.Infrastructure/MeuEstudo.Infrastructure.csproj MeuEstudo.Infrastructure/
COPY MeuEstudo.Domain/MeuEstudo.Domain.csproj MeuEstudo.Domain/

# Restaura somente a API e suas dependências transitivas (Application,
# Infrastructure e Domain). O projeto de testes não faz parte da imagem final.
RUN dotnet restore MeuEstudo.Api/MeuEstudo.Api.csproj

COPY . .

RUN dotnet publish MeuEstudo.Api/MeuEstudo.Api.csproj \
        --configuration Release \
        --output /app/publish \
        --no-restore \
        /p:UseAppHost=false

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Produção por padrão. O Render injeta a variável PORT em tempo de execução;
# 10000 é o fallback usado localmente (e o padrão do Render).
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    PORT=10000

COPY --from=build /app/publish .

# A aplicação grava uploads em wwwroot/uploads no startup (Program.cs).
# O diretório precisa existir e ser gravável pelo usuário não-root da imagem.
RUN mkdir -p /app/wwwroot/uploads/perfis /app/wwwroot/uploads/publicacoes \
    && chown -R $APP_UID:$APP_UID /app

USER $APP_UID

EXPOSE 10000

# ASPNETCORE_HTTP_PORTS (definido na imagem base como 8080) é removido para
# garantir que apenas a porta do Render seja usada. O exec mantém o dotnet
# como PID 1, permitindo desligamento gracioso no SIGTERM.
ENTRYPOINT ["sh", "-c", "unset ASPNETCORE_HTTP_PORTS; exec dotnet MeuEstudo.Api.dll --urls \"http://+:${PORT:-10000}\""]
