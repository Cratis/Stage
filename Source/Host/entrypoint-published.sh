#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e

# A published application: the Chronicle kernel and the Stage run in this container, talking over localhost, but the
# kernel keeps its data in the database the deployment configures, authenticates the Stage, and serves neither its API
# nor its Workbench. The model is the one baked into the package at Stage__Published__PackagePath; nothing can load another.
#
# Configuration (the host checks everything else and refuses to start without it):
#   Stage__Published__Storage__Type                  MongoDB (the default), PostgreSql or MsSql.
#   Stage__Published__Storage__ConnectionString      the connection string of that database, or
#   Stage__Published__Storage__ConnectionStringFile  the path of a mounted secret holding it.
#   CRATIS_CHRONICLE_CONNECTION_STRING_FILE          the path of a mounted Chronicle connection string. When it names a
#                                                    readable, non-empty file, the application uses that external kernel:
#                                                    no kernel is started here and no storage is required.
# This script never prints a connection string.

# The kernel's documented spelling, whatever case the deployment used.
storage_type="${Stage__Published__Storage__Type:-MongoDB}"
case "${storage_type,,}" in
    mongodb) storage_type=MongoDB ;;
    postgresql) storage_type=PostgreSql ;;
    mssql) storage_type=MsSql ;;
    *)
        printf 'ERROR: Stage__Published__Storage__Type is not supported. Set it to one of MongoDB, PostgreSql, MsSql.\n' >&2
        exit 78
        ;;
esac
export Stage__Published__Storage__Type="$storage_type"

external="${CRATIS_CHRONICLE_CONNECTION_STRING_FILE:-}"
if [ -n "$external" ]; then
    if [ ! -r "$external" ] || [ -z "$(tr -d '[:space:]' < "$external")" ]; then
        printf 'ERROR: CRATIS_CHRONICLE_CONNECTION_STRING_FILE is set but does not name a readable, non-empty file.\n' >&2
        exit 78
    fi
    # The host refuses this too; checking here keeps a misconfigured deployment from getting as far as starting it.
    if tr -d '\r\n' < "$external" | grep -Eiq '[?&]auth=none(&|$)'; then
        printf 'ERROR: The mounted Chronicle connection must not disable authentication (auth=none).\n' >&2
        exit 78
    fi
fi

connection="${Stage__Published__Storage__ConnectionString:-}"
if [ -z "$connection" ] && [ -n "${Stage__Published__Storage__ConnectionStringFile:-}" ]; then
    if [ ! -r "$Stage__Published__Storage__ConnectionStringFile" ]; then
        printf 'ERROR: Stage__Published__Storage__ConnectionStringFile is set but not readable.\n' >&2
        exit 78
    fi
    connection="$(tr -d '\r\n' < "$Stage__Published__Storage__ConnectionStringFile")"
fi
if [ -z "$connection" ] && [ -z "$external" ]; then
    printf 'ERROR: No persistent storage is configured. Set Stage__Published__Storage__ConnectionString or Stage__Published__Storage__ConnectionStringFile.\n' >&2
    exit 78
fi

package="${Stage__Published__PackagePath:-/stage/package}"
# The package is a folder, or a .tar.gz archive of one that the host extracts at startup.
case "$package" in
    *.tar.gz|*.tgz)
        if [ ! -f "$package" ]; then
            printf 'ERROR: No application package archive at %s.\n' "$package" >&2
            exit 78
        fi
        ;;
    *)
        if [ ! -f "$package/manifest.json" ] || [ ! -d "$package/model" ]; then
            printf 'ERROR: No application package at %s (expected manifest.json and a model folder).\n' "$package" >&2
            exit 78
        fi
        ;;
esac

if [ -n "$external" ]; then
    # The host connects with the mounted connection and still applies its guards: no auth=none, and client credentials
    # outside loopback.
    echo "Using the external Chronicle kernel from CRATIS_CHRONICLE_CONNECTION_STRING_FILE; no kernel is started here."
else
    echo "Starting Chronicle ($storage_type storage, authentication on)..."
    export Cratis__Chronicle__Storage__Type="$storage_type"
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
fi

echo "Starting Stage (published) on port 9090..."
cd /stage
export ASPNETCORE_ENVIRONMENT=Published
export Stage__Mode=Published
exec dotnet Cratis.Stage.Host.dll --published
