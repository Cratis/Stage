#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e

# This container is a self-contained play sandbox: the Chronicle kernel and the Stage run here and talk to each
# other over localhost. Storage is fully in-memory — no database is bundled — so every play session is completely
# isolated and disposable.

# 1. Start the Chronicle kernel (Cratis.Chronicle.Server lives in /app in the base image) with in-memory storage.
#    It is run from /app so its relative paths resolve correctly.
echo "Starting Chronicle (in-memory storage)..."
export Cratis__Chronicle__Storage__Type=InMemory
( cd /app && exec ./Cratis.Chronicle.Server ) &

echo "Waiting for Chronicle to be ready..."
until nc -z localhost 35000 > /dev/null 2>&1; do
    sleep 2
done
echo "Chronicle is ready."

# 2. Discover the Screenplay .play files in the mounted volume. The Stage compiles every .play file beneath
#    /eventmodel (recursively) and merges them into a single event model.
if [ -z "$(find /eventmodel -type f -name '*.play' -print -quit 2>/dev/null)" ]; then
    echo "ERROR: No Screenplay .play files found under /eventmodel/"
    exit 1
fi

echo "Using event model from Screenplay .play files under /eventmodel"

# 3. Start the Stage. It connects to the in-container Chronicle (localhost:35000) using the defaults in
#    appsettings.Docker.json and reads the event model from the mounted /eventmodel directory.
echo "Starting Stage..."
cd /stage
export ASPNETCORE_ENVIRONMENT=Docker
exec dotnet Cratis.Stage.Host.dll /eventmodel
