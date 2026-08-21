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
#
# The tag must track the Cratis.Chronicle pin in Directory.Packages.props: the kernel bundled here and the client the
# Stage is built against have to be the same version, or the two drift apart silently between releases.
FROM cratis/chronicle:16.37.0-development

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

# 9090 is the Stage API. 35000 is the Chronicle kernel's single HTTP endpoint — gRPC (HTTP/2) plus the Chronicle
# Workbench, its API and OAuth (HTTP/1.1, TLS) — so a play session can be inspected in the Workbench. Those two are
# all a caller needs; the kernel's remaining ports (Orleans clustering) stay internal.
EXPOSE 9090 35000

# Override the entrypoint to run both Chronicle and Stage
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/stage/entrypoint-stage.sh"]
