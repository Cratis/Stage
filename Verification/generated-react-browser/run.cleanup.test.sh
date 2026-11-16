#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Exercises container cleanup with a private Docker stub; no daemon, backend or browser is used.
# An optional argument selects a harness version for the regression check.
set -euo pipefail

harness=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$harness/../.." && pwd)
script=${1:-$harness/run.sh}
mkdir -p "$repo/.ai-work/stage-1153"
work=$(mktemp -d "$repo/.ai-work/stage-1153/cleanup-test.XXXXXX")
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/bin" "$work/harness/node_modules/playwright" "$work/application/wwwroot"
cp "$script" "$work/harness/run.sh"
touch "$work/application/wwwroot/index.html"

cat > "$work/bin/docker" <<'DOCKER'
#!/usr/bin/env bash
set -euo pipefail
command=$1
shift
printf '%s\n' "$command $*" >> "$CASE/commands"
case "$command" in
    run)
        cidfile=""
        label=""
        name=""
        while [ "$#" -gt 0 ]; do
            case "$1" in
                --cidfile) cidfile=$2; shift ;;
                --label) label=$2; shift ;;
                --name) name=$2; shift ;;
            esac
            shift
        done
        printf '%s\n' "$name" > "$CASE/name"
        printf '%s\n' "$cidfile" > "$CASE/cidfile"
        printf '%s\n' "$label" > "$CASE/label"
        if [ "$MODE" = clash ]; then
            echo "Conflict: the container name is already in use" >&2
            exit 125
        fi
        [ -n "$cidfile" ] || exit 91
        if [ "$MODE" = empty ]; then
            touch "$cidfile"
            exit 125
        fi
        printf '%s\n' "$ID" > "$cidfile"
        [ "$MODE" != partial ] || exit 125
        ;;
    port)
        [ "$1" = "$ID" ] || exit 92
        # No ports ends the harness before it starts a host, but after a successful create.
        ;;
    inspect)
        [ "$#" = 3 ] && [ "$1" = --format ] && [ "$3" = "$ID" ] || exit 93
        [ "$2" = '{{ index .Config.Labels "cratis.stage.verification.owner" }}' ] || exit 94
        [ "$MODE" != inspect-failed ] || exit 1
        if [ "$MODE" = mismatch ]; then
            echo another-owner
        else
            label=$(cat "$CASE/label")
            printf '%s\n' "${label#cratis.stage.verification.owner=}"
        fi
        ;;
    logs)
        [ "$#" = 1 ] && [ "$1" = "$ID" ] || exit 95
        echo 'kernel log from the owned container'
        ;;
    rm|stop)
        printf '%s\n' "$command $*" >> "$CASE/removals"
        if [ "${!#}" = "$(cat "$CASE/name")" ]; then
            rm "$CASE/existing-container"
        elif [ "${!#}" = "$ID" ]; then
            rm "$CASE/owned-container"
        fi
        ;;
    *) exit 96 ;;
esac
DOCKER
chmod +x "$work/bin/docker"

fail() {
    echo "FAIL ($MODE): $1" >&2
    cat "$CASE/output" >&2
    exit 1
}

previous_owner=""
for mode in clash empty success mismatch inspect-failed partial; do
    export MODE=$mode CASE="$work/$mode"
    export ID=0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef
    mkdir -p "$CASE/tmp"
    touch "$CASE/existing-container" "$CASE/owned-container" "$CASE/removals"
    status=0
    PATH="$work/bin:$PATH" TMPDIR="$CASE/tmp" STAGE_GENERATED_REACT_CHRONICLE_IMAGE=stub-image \
        bash "$work/harness/run.sh" "$work/application" "$CASE/result.json" > "$CASE/output" 2>&1 || status=$?
    [ "$status" = 2 ] || fail "expected could-not-run exit 2, got $status"
    [ -f "$CASE/existing-container" ] || fail 'the pre-existing named container was removed'
    case "$mode" in
        success|partial)
            [ "$(cat "$CASE/removals")" = "rm --force --volumes $ID" ] || fail 'expected exactly one removal of the recorded id'
            [ ! -f "$CASE/owned-container" ] || fail 'the owned container was not removed'
            [ "$(cat "$CASE/result.json.kernel.log")" = 'kernel log from the owned container' ] || fail 'kernel log was not preserved'
            ;;
        *)
            [ ! -s "$CASE/removals" ] || fail 'cleanup stopped or removed a container without matching ownership'
            [ -f "$CASE/owned-container" ] || fail 'an unverified container was removed'
            ;;
    esac
    case "$mode" in
        clash|empty|partial)
            grep -Fxq 'could not run: the pinned Chronicle image stub-image did not start' "$CASE/output" || fail 'create failure message changed'
            ;;
        *)
            grep -Fxq 'could not run: the Chronicle container exposed no ports' "$CASE/output" || fail 'port failure message changed'
            ;;
    esac
    cidfile=$(cat "$CASE/cidfile")
    [ -n "$cidfile" ] || fail 'no private cidfile was requested'
    [ ! -d "$(dirname "$cidfile")" ] || fail 'private temp directory was not cleaned up'
    label=$(cat "$CASE/label")
    case "$label" in
        cratis.stage.verification.owner=stage-generated-react.?*) ;;
        *) fail 'no unique ownership label was supplied' ;;
    esac
    [ "$label" != "$previous_owner" ] || fail 'ownership token was reused'
    previous_owner=$label
    echo "PASS: $mode"
done
