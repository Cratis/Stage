#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e

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

# 2. Discover the Screenplay .play files in the mounted volume. The Stage compiles every .play file beneath
#    /eventmodel (recursively) and merges them into a single event model.
stage_arguments=(/eventmodel)
if [ "${STAGE_WARM:-false}" = "true" ]; then
    stage_arguments=(--warm)
elif [ -z "$(find /eventmodel -type f -name '*.play' -print -quit 2>/dev/null)" ]; then
    echo "ERROR: No Screenplay .play files found under /eventmodel/"
    exit 1
fi

# 3. Start the Stage. A warm Stage starts without a model. Accepting a handoff exits with 42 so this supervisor
#    restarts only the Stage process against the newly written model while the in-container Chronicle stays warm.
echo "Starting Stage..."
echo "  Stage API           http://localhost:9090"
echo "  API reference       http://localhost:9090/scalar/v1"
echo "  Chronicle Workbench https://localhost:35000 — HTTPS only; plain http returns an empty response"
echo "                      sign in with the development credentials admin / ChangeMeNow!"
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
