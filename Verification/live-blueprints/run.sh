#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the Stage image against each of the seven published domain blueprints (CRM, Ticketing, Approvals, Inventory, HR, Project
# tracker, Administration): the sample application built from the blueprint's templates (Cratis/Screenplay Samples) with the approved
# package set made from the package's published manifest - blueprint and sampleData members as written - and the package's browser
# build, as `cratis run --package-set` hands them over. A browser asserts the Stage rendered every screen the blueprint's templates
# build and served none of the blueprint's sample data (Cratis/Stage#457).
#
# usage: run.sh <stage image> <results directory>
#   Needs `npm ci` and a playwright browser in ../generated-react-browser, and network access to the npm registry.
# exit:  0 every scenario passed, 1 a scenario found a defect, 2 the scenarios could not run.
set -uo pipefail

image=${1:?usage: run.sh <stage image> <results directory>}
results=${2:?usage: run.sh <stage image> <results directory>}
here=$(cd "$(dirname "$0")" && pwd)
port=${STAGE_LIVE_PORT:-19392}
container="stage-live-blueprints-$$"
work=$(mktemp -d)
mkdir -p "$results"

cleanup() { docker rm --force --volumes "$container" >/dev/null 2>&1; rm -rf "$work"; }
trap cleanup EXIT

[ -d "$here/../generated-react-browser/node_modules/playwright" ] || { echo "could not run: run 'npm ci' in generated-react-browser first"; exit 2; }

cp "$here/package.json" "$work/"
(cd "$work" && npm install --no-audit --no-fund --silent) || { echo "could not run: the blueprint packages could not be installed"; exit 2; }

count=$(node -e "console.log(require('$here/blueprints.json').length)")
status=0
for index in $(seq 0 $((count - 1))); do
    model=$(node -e "console.log(require('$here/blueprints.json')[$index].model)")
    echo "== $model"
    packages="$work/packages-$model"
    node "$here/prepare.cjs" "$work/node_modules" "$index" "$packages" || { echo "could not run: the approved set of $model could not be made"; exit 2; }

    docker rm --force --volumes "$container" >/dev/null 2>&1
    docker run --detach --name "$container" --publish "127.0.0.1:$port:9090" \
        --volume "$here/models/$model:/eventmodel:ro" \
        --volume "$packages:/packages:ro" \
        "$image" >/dev/null || { echo "could not run: the Stage image $image did not start"; exit 2; }
    ready=false
    for _ in $(seq 1 90); do
        curl --silent --fail --output /dev/null "http://127.0.0.1:$port/stage/status" && { ready=true; break; }
        sleep 2
    done
    $ready || { docker logs "$container" 2>&1 | tail -30; echo "could not run: the Stage did not become ready for $model"; exit 2; }

    STAGE_LIVE_URL="http://127.0.0.1:$port" STAGE_LIVE_NAME="$model" STAGE_LIVE_MODEL="$here/models/$model/$model.play" STAGE_LIVE_APPROVED="$packages/approved-packages.json" \
        STAGE_LIVE_RESULT="$results/live-blueprints-$model.json" node "$here/scenario.cjs"
    code=$?
    [ $code -eq 2 ] && exit 2
    [ $code -ne 0 ] && status=1
done

exit $status
