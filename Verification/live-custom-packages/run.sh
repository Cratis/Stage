#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the Stage image against a model whose ui profile names a custom runtime package (Acme.Charts), handing the container the
# approved package set and the browser build of the package the way `cratis run --package-set` does, and asserts in a browser that
# the live Stage loaded the package and rendered its component. It then starts the image again without the package folder and
# asserts the Stage refuses to render and says why - a profile is never rendered with a package silently missing.
#
# usage: run.sh <stage image> <results directory>
#   Needs `npm ci` and a playwright browser in ../generated-react-browser.
# exit:  0 every scenario passed, 1 a scenario found a defect, 2 the scenarios could not run.
set -uo pipefail

image=${1:?usage: run.sh <stage image> <results directory>}
results=${2:?usage: run.sh <stage image> <results directory>}
here=$(cd "$(dirname "$0")" && pwd)
port=${STAGE_LIVE_PORT:-19381}
container="stage-live-custom-packages-$$"
mkdir -p "$results"

cleanup() { docker rm --force --volumes "$container" >/dev/null 2>&1; }
trap cleanup EXIT

[ -d "$here/../generated-react-browser/node_modules/playwright" ] || { echo "could not run: run 'npm ci' in generated-react-browser first"; exit 2; }

start() {
    docker rm --force --volumes "$container" >/dev/null 2>&1
    docker run --detach --name "$container" --publish "127.0.0.1:$port:9090" \
        --env Stage__Runtime__Engine=eventmodel \
        --volume "$here/model:/eventmodel:ro" \
        --volume "$here/approved/approved-packages.json:/packages/approved-packages.json:ro" \
        "$@" "$image" >/dev/null || { echo "could not run: the Stage image $image did not start"; exit 2; }
    for _ in $(seq 1 90); do
        curl --silent --fail --output /dev/null "http://127.0.0.1:$port/stage/status" && return 0
        sleep 2
    done
    docker logs "$container" 2>&1 | tail -30
    echo "could not run: the Stage did not become ready"
    exit 2
}

status=0

echo "== A live Stage handed the approved package"
start --volume "$here/approved/@acme/scene.charts:/packages/@acme/scene.charts:ro"
STAGE_LIVE_URL="http://127.0.0.1:$port" STAGE_LIVE_RESULT="$results/live-custom-packages.json" node "$here/scenario.cjs"
code=$?
[ $code -eq 2 ] && exit 2
[ $code -ne 0 ] && status=1

echo "== A live Stage whose package folder is missing"
start
STAGE_LIVE_URL="http://127.0.0.1:$port" STAGE_LIVE_RESULT="$results/live-custom-packages-refusal.json" node "$here/scenario.cjs" --expect-refusal
code=$?
[ $code -eq 2 ] && exit 2
[ $code -ne 0 ] && status=1

exit $status
