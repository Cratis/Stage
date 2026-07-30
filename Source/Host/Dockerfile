# syntax=docker/dockerfile:1
# Stage image — a fully self-contained play sandbox bundling the Chronicle kernel and the Stage in one
# container. Storage is fully in-memory (no database is bundled), so every play session is completely
# isolated and disposable.
#
# The Stage app is a framework-dependent publish, built on the host (see .github/workflows/publish.yml or
# dockerize.sh) and only copied in here — its output is portable IL, so compiling it under QEMU emulation for
# the arm64 leg of a multi-arch image build would just waste time for no benefit.

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

# Copy the prebuilt Stage application
COPY Source/Host/out ./

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
