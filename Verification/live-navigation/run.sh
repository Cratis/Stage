#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the Stage image against a model with navigation regions (application topbar and sidebar, a module overriding its sidebar) and a
# toolbar button that names a State Change slice, and asserts in a browser what a user sees and does: the shell's regions swap with
# the module, the active item follows the page, a deep link restores the module and feature, the slice button opens a task dialog built
# from the command's schema and posts the command to the route the Stage registered, and the served scene carries no sample data.
#
# usage: run.sh <stage image> <results directory>
#   Needs `npm ci` and a playwright browser in ../generated-react-browser.
# exit:  0 every assertion passed, 1 an assertion failed, 2 the scenario could not run.
set -uo pipefail

image=${1:?usage: run.sh <stage image> <results directory>}
results=${2:?usage: run.sh <stage image> <results directory>}
here=$(cd "$(dirname "$0")" && pwd)
port=${STAGE_LIVE_PORT:-19391}
container="stage-live-navigation-$$"
mkdir -p "$results"

cleanup() { docker rm --force --volumes "$container" >/dev/null 2>&1; }
trap cleanup EXIT

[ -d "$here/../generated-react-browser/node_modules/playwright" ] || { echo "could not run: run 'npm ci' in generated-react-browser first"; exit 2; }

docker run --detach --name "$container" --publish "127.0.0.1:$port:9090" --volume "$here/model:/eventmodel:ro" "$image" >/dev/null \
    || { echo "could not run: the Stage image $image did not start"; exit 2; }
ready=false
deadline=$((SECONDS + 180))
for _ in $(seq 1 90); do
    remaining=$((deadline - SECONDS))
    (( remaining > 0 )) || break
    state=$(curl --silent --fail --max-time "$((remaining < 2 ? remaining : 2))" "http://127.0.0.1:$port/stage/status" | node -pe 'try { JSON.parse(require("fs").readFileSync(0, "utf8")).state || "" } catch { "" }')
    case "$state" in
        ready) ready=true; break ;;
        unsupported) docker logs "$container" 2>&1 | tail -30; echo "could not run: the Stage reported unsupported"; exit 2 ;;
    esac
    remaining=$((deadline - SECONDS))
    (( remaining > 0 )) || break
    sleep "$((remaining < 2 ? remaining : 2))"
done
$ready || { docker logs "$container" 2>&1 | tail -30; echo "could not run: the Stage did not become ready"; exit 2; }

NODE_PATH="$here/../generated-react-browser/node_modules${NODE_PATH:+:$NODE_PATH}" \
STAGE_LIVE_URL="http://127.0.0.1:$port" STAGE_LIVE_RESULT="$results/live-navigation.json" STAGE_LIVE_SCREENSHOTS="$results" \
    node "$here/scenario.cjs"
exit $?
