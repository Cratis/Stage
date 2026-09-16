#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e

# A warm container intentionally starts without a model. Cold starts validate only the selected input
# before either process starts; implementation-file references remain symbolic.
input="${1:-/eventmodel}"
if [ "$input" = "--warm" ] || [ "${STAGE_WARM:-false}" = "true" ]; then
    stage_arguments=(--warm)
else
    # Anchor relative inputs before changing the working directory for the Host.
    case "$input" in
        /*) ;;
        *) input="$PWD/$input" ;;
    esac
    if [ -d "$input" ]; then
        if [ -z "$(cd -- "$input" && find . -type f -iname '*.play' -print -quit)" ]; then
            printf 'ERROR: No Screenplay .play files found under %s. Supply a .play file or a folder containing .play files.\n' "$input" >&2
            exit 1
        fi
    elif [ -f "$input" ]; then
        case "$input" in
            *.[pP][lL][aA][yY]) ;;
            *)
                printf 'ERROR: Selected file %s must have a .play extension.\n' "$input" >&2
                exit 1
                ;;
        esac
    else
        printf 'ERROR: Input %s does not exist. Supply a .play file or a folder containing .play files.\n' "$input" >&2
        exit 1
    fi

    stage_arguments=("$input")
    printf 'Using event model from %s\n' "$input"
fi

# This container is a self-contained play sandbox: the Chronicle kernel and the Stage run here and talk to each
# other over localhost. Storage is fully in-memory — no database is bundled — so every play session is completely
# isolated and disposable.

# 1. Start the Chronicle kernel (Cratis.Chronicle.Server lives in /app in the base image) with in-memory storage.
#    It is run from /app so its relative paths resolve correctly. The Workbench (and the API it depends on) is
#    turned on explicitly rather than relying on the base image's chronicle.json, so a play session can always be
#    inspected in the Workbench on port 35000 — the same port the kernel serves gRPC on.
echo "Starting Chronicle (in-memory storage)..."
export Cratis__Chronicle__Storage__Type=InMemory
export Cratis__Chronicle__Features__Api=true
export Cratis__Chronicle__Features__Workbench=true

# No authentication. This kernel is not a server anyone can reach: it lives in this container with the one client
# that talks to it, over loopback, and dies with the container. The credential exchange would protect nothing, and
# it is expensive at exactly the wrong moment - warming the token endpoint's request pipeline for that first call
# cost 1.9 seconds on an unconstrained machine and 3.7 under the CPU limit a play session actually runs on.
# It also means the Workbench needs nobody to sign in.
export Cratis__Chronicle__Authentication__Enabled=false

( cd /app && exec ./Cratis.Chronicle.Server ) &

# Wait for the kernel to accept connections before starting the Stage. The poll is deliberately tight: the two used
# to be separated by a `sleep 2`, which on average threw away a second of startup and at worst two, purely to
# round the wait up to the next tick. A tenth of a second costs nothing measurable and returns the moment the silo
# is up.
#
# The wait is also deliberately serial. Booting the Stage alongside the kernel is faster when the container has
# more than a core to spend, but the play pod is scheduled on a 500m CPU request, and at that size the two
# processes simply take turns: measured against a 120-slice model at 0.5 CPU, overlapping them stretched the
# silo's own boot from ~6s to ~23s and the container as a whole from ~33s to ~51s. Serial is the safe shape,
# because the case it protects is a busy node - exactly when a regression hurts most.
echo "Waiting for Chronicle to be ready..."
until nc -z localhost 35000 > /dev/null 2>&1; do
    sleep 0.1
done
echo "Chronicle is ready."

# 2. Start the Stage against the selected file or folder, or without a model when warm. Accepting a handoff
#    exits with 42 so this supervisor restarts only Stage against /eventmodel while Chronicle stays warm.
echo "Starting Stage..."
echo "  Stage API           http://localhost:9090"
echo "  API reference       http://localhost:9090/scalar/v1"
echo "  Chronicle Workbench https://localhost:35000 — HTTPS only; plain http returns an empty response"
cd /stage
export ASPNETCORE_ENVIRONMENT=Docker

while true; do
    set +e
    dotnet Cratis.Stage.Host.dll "${stage_arguments[@]}"
    stage_exit_code=$?
    set -e

    if [ "$stage_exit_code" -ne 42 ]; then
        exit "$stage_exit_code"
    fi

    stage_arguments=(/eventmodel)
done
