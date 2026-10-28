#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
# Usage: run.sh <vectors-root> <results-dir>; 0 clean, 1 defects, 2 could not run.
set -euo pipefail
if [ "$#" -ne 2 ]; then
    echo 'usage: run.sh <vectors-root> <results-dir>' >&2
    exit 2
fi
java -version || exit 2
mkdir -p "$2" || exit 2
exec java "$(dirname "$0")/BindingCheck.java" "$1" "$2"
