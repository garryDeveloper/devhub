# ---------------------------------------------------------------------------
# DevHub API — production image (DEVHUB-004)
#
#   docker build -t devhub-api:local api
#   docker run --rm -p 5080:8080 \
#     -e Cors__AllowedOrigins__0=http://localhost:5173 \
#     devhub-api:local
#
# Elastic Beanstalk runs this exact image (aws-architecture.md §"Elastic Beanstalk"),
# so anything that only works outside the container is a deployment bug waiting to
# happen. No configuration and no secrets are baked in — everything comes from
# environment variables at runtime.
# ---------------------------------------------------------------------------


# --- Stage 1: build ---------------------------------------------------------
# The SDK is ~800 MB. It exists only in this stage; nothing from it reaches the
# final image except the published output copied out at the end.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, source second. Docker caches each layer against the files it
# copied, so this ordering means editing a .cs file invalidates only the layers from
# `COPY src/` down — `dotnet restore` is reused from cache. Copying the source first
# would re-download every NuGet package on every one-character change.
COPY Directory.Build.props ./
COPY src/DevHub.Domain/DevHub.Domain.csproj                 src/DevHub.Domain/
COPY src/DevHub.Application/DevHub.Application.csproj       src/DevHub.Application/
COPY src/DevHub.Infrastructure/DevHub.Infrastructure.csproj src/DevHub.Infrastructure/
COPY src/DevHub.Api/DevHub.Api.csproj                       src/DevHub.Api/

# Restore the Api project, not DevHub.sln. The solution includes
# tests/DevHub.ArchitectureTests, which .dockerignore keeps out of the context —
# and a runtime image has no use for xunit or NetArchTest. The project reference
# graph pulls Application, Infrastructure and Domain in anyway.
RUN dotnet restore src/DevHub.Api/DevHub.Api.csproj

COPY src/ src/

# Framework-dependent publish: the output is portable IL, which is why a glibc SDK
# can build for the musl runtime below. Add -r linux-musl-x64 here the day this
# moves to self-contained or AOT, and switch the SDK to 10.0-alpine to match.
RUN dotnet publish src/DevHub.Api/DevHub.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish


# --- Stage 2: runtime -------------------------------------------------------
# aspnet, not sdk: no compiler, no NuGet cache, no build tooling to exploit.
# alpine, not the Debian default: ~57 MB against ~220 MB.
#
# Note that this image sets DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true — Alpine
# ships without ICU. Fine for DevHub (ids are uuid, timestamps are UTC timestamptz,
# and the API formats nothing per-culture), but culture-aware parsing or formatting
# would silently behave differently here than on your machine.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# 8080, not 80: binding a port below 1024 requires root or CAP_NET_BIND_SERVICE,
# and the whole point of the USER line below is to need neither. The base image
# already defaults to 8080 via ASPNETCORE_HTTP_PORTS; this is stated explicitly so
# the contract is visible in the file that publishes EXPOSE 8080.
ENV ASPNETCORE_URLS=http://+:8080

WORKDIR /app

# Copied as root and left owned by root: the app user can read and execute its own
# code but cannot rewrite it. A writable application directory turns any file-write
# bug into persistent code execution.
COPY --from=build /app/publish .

EXPOSE 8080

# APP_UID (1654) is defined by the base image, whose default USER is still root.
# Beanstalk and ECS both run containers as whatever the image says, so leaving this
# out means the API runs as root on AWS for no reason.
USER $APP_UID

# wget is busybox's, already present in Alpine — no package install, no extra layer.
# --start-period gives the runtime time to JIT and bind before a failure counts.
# -qO- puts the /health payload in `docker inspect`'s health log, which is the
# fastest way to see *why* a container is unhealthy.
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "DevHub.Api.dll"]
