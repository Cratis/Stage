#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Linux/macOS boundary test; Windows is not covered. Requires Bash, Python 3, and find.
# Executes the production entrypoint with real input validation and fake processes only.
set -eu
script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
exec python3 - "$script_directory/../entrypoint-stage.sh" <<'PYTHON'
import hashlib
import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import tempfile


ENTRYPOINT = Path(sys.argv[1]).resolve(strict=True)
BASH = "/bin/bash"
FIND = shutil.which("find", path=os.defpath)
if FIND is None:
    raise SystemExit("A system find executable is required")

# No inherited startup files/functions or production PATH enter the child.
# '[' translation is necessary because the default directory test happens BEFORE cd.
# It delegates to the genuine builtin and does not turn a failed check into success.
BOOTSTRAP = r'''
cd() {
    local target
    if [[ ${1-} == -- ]]; then shift; fi
    if (( $# != 1 )); then return 97; fi
    target=$1
    case "$target" in
        /app) target=$BOUNDARY_APP ;;
        /stage) target=$BOUNDARY_STAGE ;;
        /eventmodel) target=$BOUNDARY_DEFAULT ;;
    esac
    case "$target" in
        "$BOUNDARY_ROOT"/*) builtin cd -- "$target" ;;
        /*) printf 'Unmapped absolute cd: %s\n' "$target" >&2; return 97 ;;
        *) builtin cd -- "$target" ;;
    esac
}
function [() {
    if (( $# == 3 )) && [[ $2 == /eventmodel ]] && [[ $1 == -d || $1 == -f ]]; then
        builtin [ "$1" "$BOUNDARY_DEFAULT" ']'
    else
        builtin [ "$@"
    fi
}
export -f cd '['
exec /bin/bash "$BOUNDARY_ENTRYPOINT" "$@"
'''

# Every generated executable is owned by this fixture. Even readiness is a recording
# stub, not a network operation. The kernel signals only AFTER its record is closed;
# Host waits on this inherited pipe, never a sleep or a polling loop.
FAKE = r'''
import json
import os
from pathlib import Path
import sys

kind = sys.argv[1]
arguments = sys.argv[2:]
record = {"kind": kind, "argv": arguments, "cwd": os.getcwd()}
if kind == "dotnet" and len(arguments) == 2:
    selected = arguments[1]
    if selected == "/eventmodel":
        selected = os.environ["BOUNDARY_DEFAULT"]
    record["resolved_input"] = str(Path(selected).resolve())
with open(os.environ["BOUNDARY_RECORDS"], "a", encoding="utf-8") as output:
    output.write(json.dumps(record) + "\n")
if kind == "kernel":
    os.write(int(os.environ["BOUNDARY_READY_WRITE"]), b"K")
elif kind == "dotnet":
    if os.read(int(os.environ["BOUNDARY_READY_READ"]), 1) != b"K":
        raise SystemExit("Kernel startup synchronization failed")
elif kind == "sleep":
    raise SystemExit("Readiness must succeed immediately; sleeps are forbidden")
'''


def executable(path, body):
    path.write_text(body, encoding="utf-8")
    path.chmod(0o700)


def execute(environment, arguments, cwd, read_fd, write_fd):
    # Own a separate process group so timeout cleanup targets only this invocation,
    # including a background fake kernel; never use process-name matching.
    process = subprocess.Popen(
        [BASH, "--noprofile", "--norc", "-c", BOOTSTRAP, "boundary", *arguments],
        cwd=cwd,
        env=environment,
        stdin=subprocess.DEVNULL,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        pass_fds=(read_fd, write_fd),
        start_new_session=True,
    )
    try:
        stdout, stderr = process.communicate(timeout=10)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        process.communicate(timeout=5)
        raise AssertionError("Production entrypoint exceeded the 10-second fixture deadline")
    # communicate sees EOF only after all children holding stdout/stderr have exited.
    # This also closes the invalid-case background-start race before reading records.
    return process.returncode, stdout.decode(errors="replace"), stderr.decode(errors="replace")


def run_case(root, name, selection, valid, default_has_play=True):
    case_root = root / name
    app = case_root / "app"
    stage = case_root / "stage"
    default = case_root / "eventmodel"
    work = case_root / "nondefault working directory"
    binaries = case_root / "executables"
    for directory in (app, stage, default, work, binaries):
        directory.mkdir(parents=True)
    if default_has_play:
        (default / "nested").mkdir()
        (default / "nested" / "default.PLAY").write_text("fixture", encoding="utf-8")
    folder = work / "selected folder with spaces"
    folder.mkdir()
    (folder / "nested").mkdir()
    (folder / "nested" / "selected.PLAY").write_text("fixture", encoding="utf-8")
    lower = work / "selected file.play"
    upper = work / "selected file.PLAY"
    wrong = work / "wrong.txt"
    double = work / "wrong.play.txt"
    relative = work / "relative file.play"
    for path in (lower, upper, wrong, double, relative):
        path.write_text("fixture", encoding="utf-8")
    empty = work / "empty folder"
    empty.mkdir()
    choices = {
        "default": ([], default),
        "folder": ([str(folder)], folder),
        "lower": ([str(lower)], lower),
        "upper": ([str(upper)], upper),
        "missing": ([str(work / "missing.play")], work / "missing.play"),
        "wrong": ([str(wrong)], wrong),
        "double": ([str(double)], double),
        "empty": ([str(empty)], empty),
        "relative": ([relative.name], relative),
        "relative_folder": ([folder.name], folder),
    }
    arguments, expected_target = choices[selection]
    expected_argument = arguments[0] if arguments else "/eventmodel"
    if arguments and not os.path.isabs(expected_argument):
        expected_argument = os.path.join(work, expected_argument)
    records_path = case_root / "calls.jsonl"
    fake = case_root / "fake.py"
    fake.write_text(FAKE, encoding="utf-8")
    for path, kind in (
        (app / "Cratis.Chronicle.Server", "kernel"),
        (binaries / "nc", "nc"),
        (binaries / "dotnet", "dotnet"),
        (binaries / "sleep", "sleep"),
    ):
        executable(path, '#!/bin/bash\nexec "$BOUNDARY_PYTHON" "$BOUNDARY_FAKE" ' + kind + ' "$@"\n')
    # Real find, restricted to an owned working directory. No validator replacement.
    executable(binaries / "find", '''#!/bin/bash
case "$PWD" in
    "$BOUNDARY_ROOT"/*) exec "$BOUNDARY_FIND" "$@" ;;
    *) printf 'find escaped the owned fixture\n' >&2; exit 97 ;;
esac
''')
    read_fd, write_fd = os.pipe()
    environment = {
        "PATH": str(binaries),
        "HOME": str(case_root),
        "LC_ALL": "C",
        "BOUNDARY_ROOT": str(case_root),
        "BOUNDARY_APP": str(app),
        "BOUNDARY_STAGE": str(stage),
        "BOUNDARY_DEFAULT": str(default),
        "BOUNDARY_ENTRYPOINT": str(ENTRYPOINT),
        "BOUNDARY_RECORDS": str(records_path),
        "BOUNDARY_PYTHON": sys.executable,
        "BOUNDARY_FAKE": str(fake),
        "BOUNDARY_FIND": FIND,
        "BOUNDARY_READY_READ": str(read_fd),
        "BOUNDARY_READY_WRITE": str(write_fd),
    }
    try:
        status, stdout, stderr = execute(environment, arguments, work, read_fd, write_fd)
    finally:
        os.close(read_fd)
        os.close(write_fd)
    records = [json.loads(line) for line in records_path.read_text(encoding="utf-8").splitlines()] if records_path.exists() else []
    kernels = [record for record in records if record["kind"] == "kernel"]
    hosts = [record for record in records if record["kind"] == "dotnet"]
    checks = [record for record in records if record["kind"] == "nc"]
    evidence = json.dumps({"exit": status, "calls": records, "stdout": stdout, "stderr": stderr})
    if not valid:
        assert status != 0, "Invalid input succeeded: " + evidence
        assert not kernels, "Invalid input started kernel: " + evidence
        assert not hosts and not checks, "Invalid input reached readiness/Host: " + evidence
        assert "ERROR:" in stderr, "Missing validation diagnostic: " + evidence
        return
    assert status == 0, "Valid input was rejected: " + evidence
    assert len(kernels) == 1, "Expected exactly one kernel startup: " + evidence
    assert kernels[0]["cwd"] == str(app) and kernels[0]["argv"] == [], "Kernel launch changed: " + evidence
    assert len(checks) == 1 and checks[0]["argv"] == ["-z", "localhost", "35000"], "Readiness was not immediate: " + evidence
    assert not any(record["kind"] == "sleep" for record in records), "Readiness slept: " + evidence
    assert len(hosts) == 1, "Expected exactly one Host launch: " + evidence
    assert hosts[0]["cwd"] == str(stage), "Host was not launched from /stage: " + evidence
    assert hosts[0]["argv"] == ["Cratis.Stage.Host.dll", expected_argument], "Selected input must be one Host argument anchored to its original working directory: " + evidence
    assert hosts[0]["resolved_input"] == str(expected_target), "Host input resolves to a different path after cd /stage: " + evidence


CASES = [
    ("default_folder", "default", True, True),
    ("absolute_folder_with_spaces", "folder", True, True),
    ("selected_play_file", "lower", True, True),
    ("selected_uppercase_PLAY_file", "upper", True, True),
    ("missing_selected_path", "missing", False, True),
    ("wrong_txt_suffix", "wrong", False, True),
    ("wrong_play_txt_suffix", "double", False, True),
    ("empty_selected_folder", "empty", False, True),
    ("empty_default_folder", "default", False, False),
    ("relative_file_from_nondefault_cwd", "relative", True, True),
    ("relative_folder_from_nondefault_cwd", "relative_folder", True, True),
]
print("Production entrypoint:", ENTRYPOINT)
print("Production SHA256:", hashlib.sha256(ENTRYPOINT.read_bytes()).hexdigest())
print("Linux/macOS fake-process boundary only; Windows is not covered.")
failures = []
# Cleanup is exclusively the directory created by this TemporaryDirectory instance.
with tempfile.TemporaryDirectory(prefix="stage-entrypoint-boundary-") as temporary:
    root = Path(temporary).resolve()
    for case in CASES:
        try:
            run_case(root, *case)
        except (AssertionError, OSError, ValueError) as error:
            failures.append(case[0])
            print("FAIL", case[0], str(error))
        else:
            print("PASS", case[0])
print(f"{len(CASES) - len(failures)}/{len(CASES)} cases passed")
raise SystemExit(1 if failures else 0)
PYTHON
