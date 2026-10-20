#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Starts the Stage image on model/templates.play (the default blueprint is shipped by the Stage, nothing is approved) and runs the
# blueprint-template vector against it in a browser (Cratis/Scene#95).
#
# usage: live.sh <stage image> <results directory>
#   Needs `npm ci` and a playwright browser in ../generated-react-browser.
# exit:  0 every assertion passed, 1 an assertion found a defect, 2 the scenario could not run.
set -uo pipefail

image=${1:?usage: live.sh <stage image> <results directory>}
results=${2:?usage: live.sh <stage image> <results directory>}
here=$(cd "$(dirname "$0")" && pwd)
port=${STAGE_LIVE_PORT:-19393}
container="stage-live-templates-$$"
mkdir -p "$results"

cleanup() { docker rm --force --volumes "$container" >/dev/null 2>&1; }
trap cleanup EXIT

[ -d "$here/../generated-react-browser/node_modules/playwright" ] || { echo "could not run: run 'npm ci' in generated-react-browser first"; exit 2; }

docker run --detach --name "$container" --publish "127.0.0.1:$port:9090" --volume "$here/model:/eventmodel:ro" "$image" >/dev/null \
    || { echo "could not run: the Stage image $image did not start"; exit 2; }
ready=false
for _ in $(seq 1 90); do
    curl --silent --fail --output /dev/null "http://127.0.0.1:$port/stage/status" && { ready=true; break; }
    sleep 2
done
$ready || { docker logs "$container" 2>&1 | tail -30; echo "could not run: the Stage did not become ready"; exit 2; }

STAGE_LIVE_URL="http://127.0.0.1:$port" STAGE_LIVE_RESULT="$results/live-blueprint-templates.json" node "$here/live-scenario.cjs"
