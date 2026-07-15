#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e

# This container is a self-contained play sandbox: MongoDB, the Chronicle kernel and the Stage all run
# here and talk to each other over localhost. Nothing outside the container is required, so every play
# session is fully isolated.

# 1. Start an isolated single-node MongoDB replica set (Chronicle requires multi-document transactions).
echo "Starting MongoDB..."
mkdir -p /data/db
mongod --replSet rs0 --bind_ip 127.0.0.1 --dbpath /data/db --quiet &

echo "Waiting for MongoDB to accept connections..."
until mongosh --quiet --eval "db.adminCommand('ping')" > /dev/null 2>&1; do
    sleep 1
done

mongosh --quiet --eval "try { rs.status() } catch (e) { rs.initiate({ _id: 'rs0', members: [{ _id: 0, host: '127.0.0.1:27017' }] }) }" > /dev/null 2>&1

echo "Waiting for MongoDB to elect a primary..."
until [ "$(mongosh --quiet --eval 'db.hello().isWritablePrimary' 2>/dev/null)" = "true" ]; do
    sleep 1
done
echo "MongoDB is ready."

# 2. Start the Chronicle kernel (Cratis.Chronicle.Server lives in /app in the base image) backed by the
#    local MongoDB. It is run from /app so its relative paths resolve correctly.
echo "Starting Chronicle..."
export Cratis__Chronicle__Storage__Type=MongoDB
export Cratis__Chronicle__Storage__ConnectionDetails="mongodb://127.0.0.1:27017/?directConnection=true"
( cd /app && exec ./Cratis.Chronicle.Server ) &

echo "Waiting for Chronicle to be ready..."
until curl -sf http://localhost:8080/health > /dev/null 2>&1; do
    sleep 2
done
echo "Chronicle is ready."

# 3. Find the event model JSON file in the mounted volume.
MODEL_FILE=""
if [ -f /eventmodel/model.json ]; then
    MODEL_FILE="/eventmodel/model.json"
else
    MODEL_FILE=$(find /eventmodel -maxdepth 1 -name "*.json" | head -n 1)
fi

if [ -z "$MODEL_FILE" ]; then
    echo "ERROR: No event model JSON file found in /eventmodel/"
    exit 1
fi

echo "Using event model: $MODEL_FILE"

# 4. Start the Stage. It connects to the in-container Chronicle (localhost:35000) and MongoDB
#    (localhost:27017) using the defaults in appsettings.Docker.json.
echo "Starting Stage..."
cd /stage
export ASPNETCORE_ENVIRONMENT=Docker
exec dotnet Cratis.Stage.Host.dll "$MODEL_FILE"
