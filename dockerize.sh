#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Builds the Stage Docker images locally: the Host (a self-contained play sandbox bundling the Chronicle
# kernel and the Stage engine) and the SpecRunner (a run-to-completion specification job). Both Dockerfiles
# expect a prebuilt, framework-dependent `dotnet publish` output already sitting in their own `out/` folder
# (Source/Host/out, Source/SpecRunner/out) rather than compiling inside the Docker build, so this script
# publishes both apps first and only then builds the images — always from the repository root regardless of
# where it is invoked.
#
# The Host image defaults to 'cratis/studio-stage:latest', which is exactly what Play looks for
# (Play:StageImage, defaulting to cratis/studio-stage:latest).
#
# Usage:
#   ./dockerize.sh [extra docker build args...]
#
# Environment overrides:
#   STAGE_IMAGE        Host image repository (default: cratis/studio-stage)
#   STAGE_TAG          Host primary tag (default: latest)
#   SPECRUNNER_IMAGE   SpecRunner image repository (default: cratis/stage-specrunner)
#   SPECRUNNER_TAG     SpecRunner primary tag (default: latest)
#   VERSION            version baked into both apps (default: derived from git)
#   COMMIT             commit baked into both apps (default: derived from git)
#   PLATFORM           target platform, e.g. linux/amd64 (default: the host's own architecture — linux/arm64 on Apple Silicon)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

STAGE_IMAGE="${STAGE_IMAGE:-cratis/studio-stage}"
STAGE_TAG="${STAGE_TAG:-latest}"
SPECRUNNER_IMAGE="${SPECRUNNER_IMAGE:-cratis/stage-specrunner}"
SPECRUNNER_TAG="${SPECRUNNER_TAG:-latest}"

# Derive version/commit metadata from git when not supplied. These are baked into the published assemblies.
VERSION="${VERSION:-$(git -C "${REPO_ROOT}" describe --tags --always --dirty 2>/dev/null | sed 's/^v//')}"
VERSION="${VERSION:-0.0.0-dev}"
COMMIT="${COMMIT:-$(git -C "${REPO_ROOT}" rev-parse --short HEAD 2>/dev/null || echo dev)}"

# Default the target platform to the host's own architecture so the image matches what it runs on
# (e.g. linux/arm64 on Apple Silicon, linux/amd64 on Intel/AMD). Overridable via the PLATFORM env var.
if [ -z "${PLATFORM:-}" ]; then
    case "$(uname -m)" in
        arm64 | aarch64) PLATFORM="linux/arm64" ;;
        x86_64 | amd64) PLATFORM="linux/amd64" ;;
        *) PLATFORM="" ;;
    esac
fi

echo "Publishing Host (version=${VERSION}, commit=${COMMIT})..."
dotnet publish "${REPO_ROOT}/Source/Host/Host.csproj" \
    -c Release \
    -p:Version="${VERSION}" \
    -p:SourceRevisionId="${COMMIT}" \
    -p:EnableSourceControlManagerQueries=false \
    -p:EnableSourceLink=false \
    -o "${REPO_ROOT}/Source/Host/out"

echo "Publishing SpecRunner (version=${VERSION}, commit=${COMMIT})..."
dotnet publish "${REPO_ROOT}/Source/SpecRunner/SpecRunner.csproj" \
    -c Release \
    -p:Version="${VERSION}" \
    -p:SourceRevisionId="${COMMIT}" \
    -p:EnableSourceControlManagerQueries=false \
    -p:EnableSourceLink=false \
    -o "${REPO_ROOT}/Source/SpecRunner/out"

# Assembled as an array (always non-empty) so it expands cleanly under `set -u`, including on the bash 3.2
# that ships with macOS where expanding an empty array would error.
PLATFORM_ARGS=()
if [ -n "${PLATFORM:-}" ]; then
    PLATFORM_ARGS=(--platform "${PLATFORM}")
fi

echo "Building ${STAGE_IMAGE}:${STAGE_TAG}"
docker build \
    --file "${REPO_ROOT}/Source/Host/Dockerfile" \
    --tag "${STAGE_IMAGE}:${STAGE_TAG}" \
    --tag "${STAGE_IMAGE}:${VERSION}" \
    "${PLATFORM_ARGS[@]}" \
    "$@" \
    "${REPO_ROOT}"

echo "Building ${SPECRUNNER_IMAGE}:${SPECRUNNER_TAG}"
docker build \
    --file "${REPO_ROOT}/Source/SpecRunner/Dockerfile" \
    --tag "${SPECRUNNER_IMAGE}:${SPECRUNNER_TAG}" \
    --tag "${SPECRUNNER_IMAGE}:${VERSION}" \
    "${PLATFORM_ARGS[@]}" \
    "$@" \
    "${REPO_ROOT}"

echo "Built ${STAGE_IMAGE}:${STAGE_TAG}, ${STAGE_IMAGE}:${VERSION}, ${SPECRUNNER_IMAGE}:${SPECRUNNER_TAG} and ${SPECRUNNER_IMAGE}:${VERSION}"
