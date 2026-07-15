#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Builds the self-contained Stage container image — MongoDB, the Chronicle kernel and the Stage host bundled
# in a single container — that the Studio Play system launches to run an event-model play session.
#
# The image is tagged 'cratis/studio-stage:latest' by default, which is exactly what Play looks for
# (Play:StageImage, defaulting to cratis/studio-stage:latest). The Dockerfile expects the repository root as
# its build context (it copies Directory.Build.props, Directory.Packages.props, global.json and Source/), so
# this script always builds from the repo root regardless of where it is invoked.
#
# Usage:
#   ./Source/Cratis.Stage.Host/dockerize.sh [extra docker build args...]
#
# Environment overrides:
#   STAGE_IMAGE   image repository (default: cratis/studio-stage)
#   STAGE_TAG     primary tag (default: latest)
#   VERSION       version baked into the image (default: derived from git)
#   COMMIT        commit baked into the image (default: derived from git)
#   PLATFORM      target platform, e.g. linux/amd64 (default: the host's own architecture — linux/arm64 on Apple Silicon)

set -euo pipefail

# Resolve the Cratis.Stage.Host directory and the repository root from this script's location, so the script works
# no matter the current working directory.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

IMAGE="${STAGE_IMAGE:-cratis/studio-stage}"
TAG="${STAGE_TAG:-latest}"

# Derive version/commit metadata from git when not supplied. These are passed to the Dockerfile as build args
# and stamped into the published assembly.
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

echo "Building ${IMAGE}:${TAG} (version=${VERSION}, commit=${COMMIT})"
echo "  context:    ${REPO_ROOT}"
echo "  dockerfile: ${SCRIPT_DIR}/Dockerfile"
echo "  platform:   ${PLATFORM:-host default}"

# Assemble the arguments into a single array (always non-empty) so it expands cleanly under `set -u`,
# including on the bash 3.2 that ships with macOS where expanding an empty array would error.
BUILD_ARGS=(
    --file "${SCRIPT_DIR}/Dockerfile"
    --build-arg "VERSION=${VERSION}"
    --build-arg "COMMIT=${COMMIT}"
    --tag "${IMAGE}:${TAG}"
    --tag "${IMAGE}:${VERSION}"
)

if [ -n "${PLATFORM:-}" ]; then
    BUILD_ARGS+=(--platform "${PLATFORM}")
fi

docker build "${BUILD_ARGS[@]}" "$@" "${REPO_ROOT}"

echo "Built ${IMAGE}:${TAG} and ${IMAGE}:${VERSION}"
