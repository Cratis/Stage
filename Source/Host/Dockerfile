# syntax=docker/dockerfile:1
# Stage image — a fully self-contained play sandbox bundling MongoDB, the Chronicle kernel and the
# Stage in one container. The Stage connects to Chronicle and MongoDB over localhost, so every play
# session is completely isolated with its own database and event store.

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
COPY Source/Directory.Build.props ./Source/
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
# The Chronicle base image is the kernel (Cratis.Chronicle.Server in /app). We add MongoDB on top so the
# container is self-contained — no external database or kernel is required to run a play session.
FROM cratis/chronicle:latest-development

# Install MongoDB (server + shell). The base image is Ubuntu 24.04 (noble); pull the MongoDB 8.0 packages
# so each play session runs its own isolated, in-container database with replica-set transaction support.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && curl -fsSL https://pgp.mongodb.com/server-8.0.asc -o /usr/share/keyrings/mongodb-server-8.0.asc \
    && echo "deb [ signed-by=/usr/share/keyrings/mongodb-server-8.0.asc ] https://repo.mongodb.org/apt/ubuntu noble/mongodb-org/8.0 multiverse" > /etc/apt/sources.list.d/mongodb-org-8.0.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends mongodb-org-server mongodb-mongosh \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /stage

# Copy the built Stage application
COPY --from=builder /app/out ./

# Create directories for the event model JSON and the MongoDB data. /eventmodel is plain so the model can be
# injected through the container archive API as well as bind-mounted.
RUN mkdir -p /eventmodel /data/db

# Copy Stage-specific configuration
COPY Source/Host/appsettings.Docker.json /stage/appsettings.Docker.json
COPY Source/Host/entrypoint-stage.sh /stage/entrypoint-stage.sh
RUN chmod +x /stage/entrypoint-stage.sh

# Expose additional port for Stage API (Chronicle uses 8080, 11111, 30000, 35000)
EXPOSE 9090

# Override the entrypoint to run both Chronicle and Stage
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/stage/entrypoint-stage.sh"]
