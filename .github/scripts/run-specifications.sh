#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
set -euo pipefail

if (( $# == 0 )); then
    echo "Usage: run-specifications.sh <project.csproj> [--no-build] [xUnit runner options]" >&2
    exit 2
fi

project="$1"
shift
runner=(dotnet run --project "$project" --configuration Debug)
if [[ "${1:-}" == "--no-build" ]]; then
    runner+=(--no-build)
    shift
fi
runner+=(--)
case "$(basename "$project")" in
    Host.csproj|SpecRunner.csproj) runner+=(--specifications) ;;
esac

result_path=
listing=false
arguments=("$@")
for (( index=0; index<${#arguments[@]}; index++ )); do
    case "${arguments[index]}" in
        -list) listing=true ;;
        -result-trx)
            if [[ -n "$result_path" || -z "${arguments[index+1]:-}" ]]; then
                echo "Expected one -result-trx result path" >&2
                exit 2
            fi
            result_path="${arguments[index+1]}"
            index=$((index+1))
            ;;
    esac
done

if [[ "$listing" == false ]]; then
    if ! command -v python3 >/dev/null; then
        echo "python3 is required to validate specification TRX results" >&2
        exit 2
    fi
    if [[ -z "$result_path" ]]; then
        temporary=$(mktemp "${TMPDIR:-/tmp}/stage-specifications.XXXXXX")
        trap 'rm -f "$temporary"' EXIT
        result_path="$temporary"
        arguments+=(-result-trx "$result_path")
    fi
    # A stale result must never make an empty or interrupted invocation look successful.
    rm -f "$result_path"
fi

"${runner[@]}" -noLogo -noColor "${arguments[@]}"
if [[ "$listing" == false ]]; then
    python3 - "$result_path" <<'PY'
import sys
import xml.etree.ElementTree as ET

try:
    root = ET.parse(sys.argv[1]).getroot()
    counters = root.findall("./{*}ResultSummary/{*}Counters")
    if root.tag.rsplit("}", 1)[-1] != "TestRun" or len(counters) != 1:
        raise ValueError("Expected one TRX result summary counter")
    if int(counters[0].get("total", "0")) <= 0:
        raise ValueError("Expected a positive TRX test total")
except (OSError, ET.ParseError, ValueError) as error:
    print(f"Invalid TRX: {error}", file=sys.stderr)
    sys.exit(1)
PY
fi
