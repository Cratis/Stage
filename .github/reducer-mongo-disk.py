#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fail-closed filesystem evidence for the CI-only reducer/Mongo runtime proofs."""

import json
import os
from pathlib import Path
import subprocess
import sys

GIB = 1024 ** 3
FLOOR, PREFLIGHT, GROWTH_LIMIT = 25 * GIB, 37 * GIB, 6 * GIB


def checked(command, timeout=10, env=None):
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=timeout, env=env)
    except subprocess.TimeoutExpired as error:
        # Keep partial failure output, bounded just like completed probe diagnostics.
        output = f"{error.stdout!r}\n{error.stderr!r}"[:65536]
        raise RuntimeError(f"Filesystem probe timed out after {timeout}s: {output}") from error
    if len(result.stdout) + len(result.stderr) > 65536:
        raise RuntimeError("Filesystem evidence exceeded its diagnostic bound.")
    if result.returncode:
        raise RuntimeError(f"Filesystem probe failed ({result.returncode}): {result.stdout}\n{result.stderr}")
    return result.stdout


def scratch_boundary(env):
    runner = Path(env["RUNNER_TEMP"]).resolve(strict=True)
    workspace = Path(env["GITHUB_WORKSPACE"]).resolve()
    scratch = Path(env["TMPDIR"])
    if runner.is_relative_to(workspace):
        raise RuntimeError("Scratch must be a private RUNNER_TEMP child outside checkout.")
    if scratch.is_symlink() or not scratch.is_absolute():
        raise RuntimeError("Scratch must be a private RUNNER_TEMP child outside checkout.")
    if scratch.resolve(strict=True).parent != runner or not scratch.name.startswith("stage342-tmp."):
        raise RuntimeError("Scratch must be a private RUNNER_TEMP child outside checkout.")
    return scratch


def owned_temp(env):
    scratch = scratch_boundary(env)
    marker = scratch / ".stage342-owner"
    if marker.is_symlink() or marker.read_text() != f'{env["GITHUB_RUN_ID"]}:{env["GITHUB_RUN_ATTEMPT"]}':
        raise RuntimeError("Scratch ownership evidence is missing or changed.")
    if any(Path(env[name]).resolve(strict=True) != scratch.resolve() for name in ("TMPDIR", "TMP", "TEMP")):
        raise RuntimeError("All host temp variables must resolve to the owned scratch.")
    return scratch.resolve()


def verify_temp(env):
    scratch = owned_temp(env)
    # A fresh interpreter exercises the resolver (not tempfile's cached path) and a real write.
    resolved = checked([sys.executable, "-c", "import pathlib, tempfile; "
                        "p = pathlib.Path(tempfile.gettempdir()).resolve(); "
                        "f = tempfile.TemporaryFile(); f.write(b'stage342'); f.flush(); f.close(); print(p)"], env=env).strip()
    if Path(resolved) != scratch:
        raise RuntimeError("Python temp resolution escaped the owned scratch.")
    return scratch


def nuget_locations():
    output = checked(["dotnet", "nuget", "locals", "all", "--list", "--force-english-output"])
    paths = {}
    for line in output.splitlines():
        name, separator, path = line.partition(": ")
        if separator and name in ("global-packages", "http-cache", "temp", "plugins-cache"):
            if name in paths or not Path(path).is_absolute():
                raise RuntimeError("Ambiguous NuGet writable path evidence.")
            paths[name] = path
    if set(paths) != {"global-packages", "http-cache", "temp", "plugins-cache"}:
        raise RuntimeError("Incomplete resolved NuGet writable path evidence.")
    return paths


def runner_locations(env):
    scratch = verify_temp(env)
    workspace = Path(env["GITHUB_WORKSPACE"])
    paths = [workspace, Path(env["RUNNER_TEMP"]), Path(env["HOME"]), scratch,
             workspace / ".ai-work/342/ci", workspace / "TestResults/reducer-mongo"]
    paths += [Path(path) for path in nuget_locations().values()]
    paths += [Path(env[name]) for name in ("NUGET_PACKAGES", "DOTNET_CLI_HOME") if env.get(name)]
    for path in paths:
        if not path.is_absolute():
            raise RuntimeError("Writable host paths must be absolute.")
        path.mkdir(parents=True, exist_ok=True)
    # Preserve distinct locations even when their backing filesystem is shared.
    return list(dict.fromkeys(str(path) for path in paths))


def writable_mounts(kernel):
    mounts = kernel["Mounts"]
    if len(mounts) > 16 or any(m["Type"] not in ("bind", "volume") for m in mounts):
        raise RuntimeError("Unsupported kernel storage mapping; cannot establish disk evidence.")
    writable = [m["Destination"] for m in mounts if m["RW"]]
    if not {"/data/db", "/data/configdb"}.issubset(writable):
        raise RuntimeError("Bundled MongoDB writable volume evidence is unavailable.")
    return writable


def container_locations(kernel):
    if not kernel["State"]["Running"]:
        raise RuntimeError("The job-owned pinned kernel must already be running.")
    configured = [entry.split("=", 1)[1] for entry in kernel["Config"]["Env"]
                  if entry.split("=", 1)[0] in ("TMPDIR", "TMP", "TEMP")]
    paths = ["/", "/tmp"] + writable_mounts(kernel) + configured
    if any(not path.startswith("/") or "\n" in path for path in paths):
        raise RuntimeError("Unknown container temp/storage mapping.")
    return list(dict.fromkeys(paths))


def capacity(domain, path, target, row):
    fsid, fragment, available, total = row.split()
    fragment, available, total = int(fragment), int(available), int(total)
    if int(fsid, 16) == 0 or fragment <= 0 or not 0 <= available <= total or total <= 0:
        raise RuntimeError("Invalid filesystem capacity/identity evidence.")
    return f"{domain}:{path}:{target}:{fsid}:{fragment}:{total}", available * fragment


def measure_paths(domain, paths, kernel_id):
    prefix = ["docker", "exec", kernel_id] if domain == "kernel" else []
    resolved = checked(prefix + ["realpath", "-e", "--", *paths]).splitlines()
    rows = checked(prefix + ["stat", "-f", "-c", "%i %S %a %b", "--", *resolved]).splitlines()
    if len(rows) != len(paths) or len(resolved) != len(paths):
        raise RuntimeError("Incomplete runner/kernel filesystem evidence.")
    if any(not p.startswith("/") for p in resolved):
        raise RuntimeError("Incomplete runner/kernel filesystem evidence.")
    return dict(capacity(domain, path, target, row) for path, target, row in zip(paths, resolved, rows))


class DiskGuard:
    def __init__(self, locations, kernel_id, evidence):
        self.locations, self.kernel_id, self.evidence = locations, kernel_id, Path(evidence)
        self.kernel_mapping = self.inspect_kernel() if "kernel" in locations else None
        self.baseline = self.measure()
        self.peak_growth = 0
        (self.evidence / "disk-baseline.json").write_text(json.dumps({
            "locations": locations, "availableBytes": self.baseline}, indent=2))
        if not self.baseline or min(self.baseline.values()) < PREFLIGHT:
            raise RuntimeError("Stage 342 needs >=37 GiB free on every actual writable filesystem (25 GiB floor + 12 GiB headroom).")

    def inspect_kernel(self):
        kernel = json.loads(checked(["docker", "inspect", self.kernel_id]))[0]
        if kernel["Id"] != self.kernel_id or container_locations(kernel) != self.locations["kernel"]:
            raise RuntimeError("Writable container mapping changed or is unknown.")
        return {"Mounts": kernel["Mounts"], "Paths": self.locations["kernel"]}

    def measure(self):
        if self.kernel_mapping is not None and self.inspect_kernel() != self.kernel_mapping:
            raise RuntimeError("Writable container mapping changed during the runtime proof.")
        filesystems = {}
        for domain, paths in self.locations.items():
            filesystems.update(measure_paths(domain, paths, self.kernel_id))
        return filesystems

    def check(self):
        current = self.measure()
        if current.keys() != self.baseline.keys():
            raise RuntimeError("Writable filesystem mapping changed during the runtime proof.")
        # Count each location, even on shared backing disks: deduplication could hide growth.
        growth = sum(max(0, self.baseline[k] - current[k]) for k in self.baseline)
        self.peak_growth = max(self.peak_growth, growth)
        (self.evidence / "disk-latest.json").write_text(json.dumps({
            "availableBytes": current, "peakGrowthUpperBoundBytes": self.peak_growth}, indent=2))
        if min(current.values()) < FLOOR or self.peak_growth > GROWTH_LIMIT:
            raise RuntimeError("Stage 342 exceeded its 25 GiB floor or 6 GiB task-growth upper bound.")


def probe_dotnet(env):
    scratch = verify_temp(env)
    paths = nuget_locations()
    DiskGuard({"runner": runner_locations(env)}, None, scratch)
    project = scratch / "dotnet-probe"
    project.mkdir()
    (project / "Probe.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
        '<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>'
        '</PropertyGroup></Project>')
    (project / "Program.cs").write_text('''using System;
using System.IO;
var temp = Path.GetTempPath();
using (var file = new FileStream(Path.Combine(temp, Guid.NewGuid().ToString()), FileMode.CreateNew,
    FileAccess.Write, FileShare.None, 4096, FileOptions.DeleteOnClose)) { file.WriteByte(42); file.Flush(true); }
File.WriteAllText(args[0], temp);
''')
    result = scratch / "dotnet-temp.txt"
    try:
        log = checked(["dotnet", "run", "--project", str(project / "Probe.csproj"), "--configuration", "Release",
                       "--verbosity", "quiet", "--", str(result)], timeout=90, env=env)
    except Exception as error:
        (scratch / "dotnet-probe.log").write_text(str(error))
        raise
    (scratch / "dotnet-probe.log").write_text(log)
    if Path(result.read_text()).resolve(strict=True) != scratch:
        raise RuntimeError(".NET temp resolution escaped the owned scratch.")
    (scratch / "nuget-paths.json").write_text(json.dumps(paths, indent=2))


def runtime_locations(env):
    scratch = verify_temp(env)
    if Path((scratch / "dotnet-temp.txt").read_text()).resolve(strict=True) != scratch:
        raise RuntimeError(".NET temp write evidence is unavailable or changed.")
    if json.loads((scratch / "nuget-paths.json").read_text()) != nuget_locations():
        raise RuntimeError("Resolved NuGet writable mappings changed after setup.")
    return runner_locations(env)


def cleanup(env):
    scratch = owned_temp(env)
    if env["JOB_STATUS"] != "success" or set(scratch.iterdir()) != {scratch / ".stage342-owner"}:
        print(f"Preserving nonempty or failed scratch for diagnosis: {scratch}")
        return
    (scratch / ".stage342-owner").unlink()
    scratch.rmdir()


if __name__ == "__main__":
    {"verify-temp": verify_temp, "probe-dotnet": probe_dotnet, "cleanup": cleanup}[sys.argv[1]](os.environ)
