#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the Stage image against each of the fifteen published domain blueprints (CRM, Ticketing, Approvals, Inventory, HR, Project
# tracker, Administration, Orders, Catalog, Vendors, Invoicing, Customer portal, Contracts, Work orders, Bookings): the sample application built from the blueprint's templates (Cratis/Screenplay Samples) with the approved
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
# The registry fails transiently now and then, and a dependency published minutes ago can be listed before every registry
# node serves it (npm ETARGET). Retry with growing waits and fresh metadata, and keep npm's own error in the log each time.
# Waits are overridable for testing; an install that never succeeds still means the scenarios could not run.
install_packages() { (cd "$work" && npm install --no-audit --no-fund --loglevel=error "$@"); }
installed=false
install_packages && installed=true
for wait in ${STAGE_LIVE_INSTALL_RETRY_WAITS:-15 30 60 120}; do
    $installed && break
    echo "the blueprint package install failed; retrying in ${wait}s with fresh registry metadata"
    sleep "$wait"
    install_packages --prefer-online && installed=true
done
$installed || { echo "could not run: the blueprint packages could not be installed"; exit 2; }

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
    deadline=$((SECONDS + 180))
    for _ in $(seq 1 90); do
        remaining=$((deadline - SECONDS))
        (( remaining > 0 )) || break
        state=$(curl --silent --fail --max-time "$((remaining < 2 ? remaining : 2))" "http://127.0.0.1:$port/stage/status" | node -pe 'try { JSON.parse(require("fs").readFileSync(0, "utf8")).state || "" } catch { "" }')
        case "$state" in
            ready) ready=true; break ;;
            unsupported) docker logs "$container" 2>&1 | tail -30; echo "could not run: the Stage reported unsupported for $model"; exit 2 ;;
        esac
        remaining=$((deadline - SECONDS))
        (( remaining > 0 )) || break
        sleep "$((remaining < 2 ? remaining : 2))"
    done
    $ready || { docker logs "$container" 2>&1 | tail -30; echo "could not run: the Stage did not become ready for $model"; exit 2; }

    STAGE_LIVE_URL="http://127.0.0.1:$port" STAGE_LIVE_NAME="$model" STAGE_LIVE_MODEL="$here/models/$model/$model.play" STAGE_LIVE_APPROVED="$packages/approved-packages.json" \
        STAGE_LIVE_RESULT="$results/live-blueprints-$model.json" node "$here/scenario.cjs"
    code=$?
    [ $code -eq 2 ] && exit 2
    [ $code -ne 0 ] && status=1
done

exit $status
