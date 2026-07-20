# syntax=docker/dockerfile:1
# Stage image — a fully self-contained play sandbox bundling the Chronicle kernel and the Stage in one
# container. Storage is fully in-memory (no database is bundled), so every play session is completely
# isolated and disposable.

ARG VERSION=0.0.0-dev
ARG COMMIT=dev

####################################
# Stage 1: Build Stage
####################################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

ARG VERSION
ARG COMMIT

WORKDIR /src

# Copy project files and restore
COPY Directory.Build.props Directory.Packages.props global.json .editorconfig .globalconfig ./
COPY Source/Directory.Build.props Source/Directory.Build.targets ./Source/
COPY Source/Contracts/Contracts.csproj ./Source/Contracts/
COPY Source/Stage/Stage.csproj ./Source/Stage/
COPY Source/Host/Host.csproj ./Source/Host/

RUN dotnet restore ./Source/Host/Host.csproj -p:Configuration=Release

# Copy source files and build
COPY README.md ./
COPY Source/Contracts/ ./Source/Contracts/
COPY Source/Stage/ ./Source/Stage/
COPY Source/Host/ ./Source/Host/

RUN dotnet publish ./Source/Host/Host.csproj \
    -c Release \
    -p:Version=${VERSION} \
    -p:SourceRevisionId=${COMMIT} \
    -p:EnableSourceControlManagerQueries=false \
    -p:EnableSourceLink=false \
    -o /app/out

####################################
# Stage 2: Runtime
####################################
# The Chronicle base image is the kernel (Cratis.Chronicle.Server in /app). Storage is fully in-memory, so no
# database is required to run a play session — the container is self-contained on its own.
FROM cratis/chronicle:latest-development

# The Chronicle base image bundles MongoDB (its default storage backend, an ~800MB install). The Stage runs
# fully in-memory, so purge it to keep the image database-free. The purge is guarded so it is a no-op on
# base-image variants that ship without MongoDB. (The base image already provides curl and netcat, which the
# entrypoint uses to wait for the Chronicle gRPC endpoint.)
RUN apt-get update \
    && (apt-get purge -y 'mongodb-org*' || true) \
    && apt-get autoremove -y \
    && rm -rf /var/lib/apt/lists/* /data/db /data/configdb

WORKDIR /stage

# Copy the built Stage application
COPY --from=builder /app/out ./

# Create the directory the Screenplay .play files are provided in. /eventmodel is plain so the model can be
# injected through the container archive API as well as bind-mounted.
RUN mkdir -p /eventmodel

# Copy Stage-specific configuration
COPY Source/Host/appsettings.Docker.json /stage/appsettings.Docker.json
COPY Source/Host/entrypoint-stage.sh /stage/entrypoint-stage.sh
RUN chmod +x /stage/entrypoint-stage.sh

# Expose additional port for Stage API (Chronicle uses 8080, 11111, 30000, 35000)
EXPOSE 9090

# Override the entrypoint to run both Chronicle and Stage
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/stage/entrypoint-stage.sh"]
