#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Serves a rendered, built screen-composition application on its generated backend against a fresh pinned Chronicle
# container, runs the browser scenarios against it and removes everything it started.
#
# usage: run.sh <rendered application directory> <result json path>
#   The application must already be built: `dotnet build Workspaces.csproj -c Release` and `npm run build`.
# exit:  0 every scenario passed, 1 a scenario found a defect, 2 the scenarios could not run.
set -uo pipefail

application=${1:?usage: run.sh <application directory> <result json path>}
result=${2:?usage: run.sh <application directory> <result json path>}
harness=$(cd "$(dirname "$0")" && pwd)
chronicle_image=${STAGE_GENERATED_REACT_CHRONICLE_IMAGE:-cratis/chronicle:19.32.0-development}
container="stage-generated-react-$$"
port=${STAGE_GENERATED_REACT_PORT:-19371}
host_pid=""

cleanup() {
    [ -n "$host_pid" ] && kill "$host_pid" 2>/dev/null && wait "$host_pid" 2>/dev/null
    docker rm --force --volumes "$container" >/dev/null 2>&1
}
trap cleanup EXIT

could_not_run() {
    echo "could not run: $1"
    exit 2
}

[ -f "$application/wwwroot/index.html" ] || could_not_run "$application/wwwroot/index.html is missing; build the frontend first"
[ -d "$harness/node_modules/playwright" ] || could_not_run "run 'npm ci' in $harness first"

docker run --detach --name "$container" --publish 127.0.0.1::27017 --publish 127.0.0.1::35000 "$chronicle_image" >/dev/null \
    || could_not_run "the pinned Chronicle image $chronicle_image did not start"
mongo_port=$(docker port "$container" 27017/tcp | head -n 1 | sed 's/.*://')
chronicle_port=$(docker port "$container" 35000/tcp | head -n 1 | sed 's/.*://')
[ -n "$mongo_port" ] && [ -n "$chronicle_port" ] || could_not_run "the Chronicle container exposed no ports"

(
    cd "$application" || exit 2
    Cratis__Chronicle__ConnectionString="chronicle://chronicle-dev-client:chronicle-dev-secret@127.0.0.1:$chronicle_port" \
    Cratis__MongoDB__Server="mongodb://127.0.0.1:$mongo_port" \
    exec dotnet run --project Workspaces.csproj -c Release --no-build --urls "http://127.0.0.1:$port"
) > "$result.host.log" 2>&1 &
host_pid=$!

ready=0
for _ in $(seq 1 90); do
    if [ "$(curl --silent --output /dev/null --write-out '%{http_code}' "http://127.0.0.1:$port/healthz")" = "200" ]; then
        ready=1
        break
    fi
    kill -0 "$host_pid" 2>/dev/null || break
    sleep 2
done
[ "$ready" = 1 ] || could_not_run "the generated host did not become healthy; see $result.host.log"

STAGE_GENERATED_REACT_URL="http://127.0.0.1:$port" \
STAGE_GENERATED_REACT_APPLICATION="$application" \
STAGE_GENERATED_REACT_RESULT="$result" \
    node "$harness/screen-composition.cjs"
exit $?
