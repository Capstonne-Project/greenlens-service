# =============================================================================
# Greenlens API — Dockerfile (multi-stage, non-root, healthchecked)
# Project: SU26SE049
#
# Build:   docker build -t greenlens-api:local .
# Run:     docker run -p 8080:8080 greenlens-api:local
# Compose: see docker-compose.yml — này không phải standalone, expects DB at $ConnectionStrings__Postgres
#
# Image size target: ~120MB (aspnet runtime ~80MB + app ~40MB)
# =============================================================================

# --------- Stage 1: restore (cached separately so source changes don't bust dotnet restore) ---------
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS restore
WORKDIR /src

# Copy ONLY .csproj first — dotnet restore is the slow step, layer-cached on csproj hash
COPY *.sln ./
COPY src/Greenlens.Domain/*.csproj            src/Greenlens.Domain/
COPY src/Greenlens.Application/*.csproj       src/Greenlens.Application/
COPY src/Greenlens.Infrastructure/*.csproj    src/Greenlens.Infrastructure/
COPY src/Greenlens.Api/*.csproj               src/Greenlens.Api/
# tests are NOT needed in production image but restore them too if you run tests in same Dockerfile
# COPY tests/Greenlens.Domain.UnitTests/*.csproj            tests/Greenlens.Domain.UnitTests/
# COPY tests/Greenlens.Application.IntegrationTests/*.csproj tests/Greenlens.Application.IntegrationTests/

RUN dotnet restore src/Greenlens.Api/Greenlens.Api.csproj


# --------- Stage 2: build + publish ---------
FROM restore AS build
WORKDIR /src
COPY . .

RUN dotnet publish src/Greenlens.Api/Greenlens.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true


# --------- Stage 3: runtime (alpine + aspnet) ---------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install curl for healthcheck + tzdata for Vietnam timezone
RUN apk add --no-cache curl tzdata icu-libs \
    && cp /usr/share/zoneinfo/Asia/Ho_Chi_Minh /etc/localtime \
    && echo "Asia/Ho_Chi_Minh" > /etc/timezone

# Globalization (Mapster, FluentValidation messages, etc.) require this
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Non-root user — never run prod containers as root
RUN addgroup -S -g 1000 app && adduser -S -G app -u 1000 app
USER app:app

COPY --from=build --chown=app:app /app/publish ./

# Internal port — exposed via compose ports binding (which binds to 127.0.0.1 only)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Healthcheck — must match the endpoint registered in Program.cs (see OVERVIEW.md §9)
# - interval 30s × retries 3 × timeout 5s = ~105s grace before unhealthy
# - start_period 60s for migration + warmup
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Greenlens.Api.dll"]
