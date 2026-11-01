#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e

# A published application: the Chronicle kernel and the Stage run in this container, talking over localhost, but the
# kernel keeps its data in the MongoDB the deployment configures, authenticates the Stage, and serves neither its API
# nor its Workbench. The model is the one baked into the package at Stage__Published__PackagePath; nothing can load another.
#
# Required configuration (the host checks everything else and refuses to start without it):
#   Stage__Published__Storage__ConnectionString      a MongoDB connection string, or
#   Stage__Published__Storage__ConnectionStringFile  the path of a mounted secret holding it.
# This script never prints the connection string.

connection="${Stage__Published__Storage__ConnectionString:-}"
if [ -z "$connection" ] && [ -n "${Stage__Published__Storage__ConnectionStringFile:-}" ]; then
    if [ ! -r "$Stage__Published__Storage__ConnectionStringFile" ]; then
        printf 'ERROR: Stage__Published__Storage__ConnectionStringFile is set but not readable.\n' >&2
        exit 78
    fi
    connection="$(tr -d '\r\n' < "$Stage__Published__Storage__ConnectionStringFile")"
fi
if [ -z "$connection" ]; then
    printf 'ERROR: No persistent storage is configured. Set Stage__Published__Storage__ConnectionString or Stage__Published__Storage__ConnectionStringFile.\n' >&2
    exit 78
fi

package="${Stage__Published__PackagePath:-/stage/package}"
if [ ! -f "$package/manifest.json" ] || [ ! -d "$package/model" ]; then
    printf 'ERROR: No application package at %s (expected manifest.json and a model folder).\n' "$package" >&2
    exit 78
fi

echo "Starting Chronicle (persistent storage, authentication on)..."
export Cratis__Chronicle__Storage__Type=MongoDB
export Cratis__Chronicle__Storage__ConnectionDetails="$connection"
export Cratis__Chronicle__Features__Api=false
export Cratis__Chronicle__Features__Workbench=false
export Cratis__Chronicle__Authentication__Enabled=true

( cd /app && exec ./Cratis.Chronicle.Server ) &

echo "Waiting for Chronicle to be ready..."
until nc -z localhost 35000 > /dev/null 2>&1; do
    sleep 0.1
done
echo "Chronicle is ready."

echo "Starting Stage (published) on port 9090..."
cd /stage
export ASPNETCORE_ENVIRONMENT=Published
export Stage__Mode=Published
exec dotnet Cratis.Stage.Host.dll --published
