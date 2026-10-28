#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Execute the reducer/Mongo disk guard without Docker, .NET builds or live integration."""

import contextlib
import io
import json
import os
from pathlib import Path
import re
import runpy
import subprocess
import sys
import tempfile
import textwrap
import unittest
from unittest.mock import patch

DISK = runpy.run_path(str(Path(__file__).with_name("reducer-mongo-disk.py")))
WORKFLOW = Path(__file__).with_name("workflows").joinpath("dotnet-build.yml").read_text()
JOB = WORKFLOW.split("  reducer-mongo:\n", 1)[1].split("  generated-name-matrix:\n", 1)[0]
GIB = 1024 ** 3


class DiskTests(unittest.TestCase):
    def setUp(self):
        work = Path(__file__).resolve().parent.parent / ".ai-work/train8/8b/disk-fix"
        work.mkdir(parents=True, exist_ok=True)
        self.task = tempfile.TemporaryDirectory(prefix="regression-", dir=work)
        self.addCleanup(self.task.cleanup)
        self.root = Path(self.task.name)
        self.env = dict(os.environ, GITHUB_WORKSPACE=str(self.root / "checkout"),
                        RUNNER_TEMP=str(self.root / "runner"), HOME=str(self.root / "home"))
        for name in ("NUGET_PACKAGES", "DOTNET_CLI_HOME"):
            self.env.pop(name, None)
        for name in ("GITHUB_WORKSPACE", "RUNNER_TEMP", "HOME"):
            Path(self.env[name]).mkdir()
        self.scratch = Path(tempfile.mkdtemp(prefix="stage342-tmp.", dir=self.env["RUNNER_TEMP"]))
        self.env.update({name: str(self.scratch) for name in ("TMPDIR", "TMP", "TEMP")})
        self.env.update(GITHUB_RUN_ID="123", GITHUB_RUN_ATTEMPT="2")
        (self.scratch / ".stage342-owner").write_text("123:2")
        self.kernel = {"Id": "kernel", "State": {"Running": True}, "Config": {"Env": []}, "Mounts": [
            {"Type": "volume", "Destination": path, "RW": True} for path in ("/data/db", "/data/configdb")]}
        self.free = {}
        self.fsid = "123"
        self.missing = False

    def stat(self, command):
        if "inspect" in command:
            return json.dumps([self.kernel])
        paths = command[command.index("--") + 1:]
        if "realpath" in command:
            return "\n".join(paths)
        if self.missing:
            return ""
        return "\n".join(f"{self.fsid} 1 {self.free.get(path, 80 * GIB)} {100 * GIB}" for path in paths)

    def guard(self, locations):
        with patch.dict(DISK["DiskGuard"].measure.__globals__, {"checked": self.stat}):
            return DISK["DiskGuard"](locations, "kernel", self.root)

    def test_resolved_roomy_scratch_does_not_measure_unrelated_small_host_tmp(self):
        caches = {name: str(self.root / name) for name in ("global-packages", "http-cache", "temp", "plugins-cache")}
        with patch.dict(DISK["runner_locations"].__globals__, {"nuget_locations": lambda: caches}):
            paths = DISK["runner_locations"](self.env)
        self.assertNotIn("/tmp", paths)
        self.assertTrue(set(caches.values()).issubset(paths), paths)
        self.assertIn(str(self.scratch), paths)
        self.free["/tmp"] = 7 * GIB
        self.guard({"runner": paths})

    def test_container_always_measures_tmp_and_configured_temp_without_host_assumptions(self):
        self.kernel["Config"]["Env"] = ["TMPDIR=/private/tmp", "TMP=/other/tmp", "TEMP=/third/tmp"]
        paths = DISK["container_locations"](self.kernel)
        for path in ("/", "/tmp", "/private/tmp", "/other/tmp", "/third/tmp", "/data/db", "/data/configdb"):
            self.assertIn(path, paths)

    def test_low_actual_temp_cache_and_container_are_rejected_with_baseline_retained(self):
        for path in (str(self.scratch), str(self.root / "http-cache"), "/tmp"):
            with self.subTest(path=path):
                self.free = {path: 36 * GIB}
                domain = "kernel" if path == "/tmp" else "runner"
                paths = DISK["container_locations"](self.kernel) if domain == "kernel" else [path]
                with self.assertRaisesRegex(RuntimeError, "37 GiB"):
                    self.guard({domain: paths})
                self.assertTrue((self.root / "disk-baseline.json").is_file())

    def test_shared_filesystems_are_counted_per_location_and_floor_is_unchanged(self):
        guard = self.guard({"runner": ["/a", "/b", "/c"]})
        self.free = dict.fromkeys(("/a", "/b", "/c"), 77 * GIB)
        with patch.dict(guard.measure.__globals__, {"checked": self.stat}), self.assertRaisesRegex(RuntimeError, "6 GiB"):
            guard.check()
        self.assertEqual(9 * GIB, guard.peak_growth)
        guard = self.guard({"runner": ["/a"]})
        self.free["/a"] = 24 * GIB
        with patch.dict(guard.measure.__globals__, {"checked": self.stat}), self.assertRaisesRegex(RuntimeError, "25 GiB"):
            guard.check()
        self.assertTrue((self.root / "disk-latest.json").is_file())

    def test_mapping_changes_and_incomplete_measurements_fail_closed(self):
        guard = self.guard({"runner": ["/a"]})
        self.fsid = "456"
        with patch.dict(guard.measure.__globals__, {"checked": self.stat}), self.assertRaisesRegex(RuntimeError, "mapping changed"):
            guard.check()
        self.missing = True
        with self.assertRaisesRegex(RuntimeError, "Incomplete"):
            self.guard({"runner": ["/a"]})

    def test_unsupported_container_mappings_fail_closed(self):
        for change in ({"Mounts": []}, {"State": {"Running": False}}, {"Mounts": [{"Type": "tmpfs"}]}):
            with self.subTest(change=change), self.assertRaises(RuntimeError):
                DISK["container_locations"](dict(self.kernel, **change))

    def bootstrap(self, available=80 * GIB):
        script = re.search(r"python3 - <<'PY'\n(.*?)\n          PY", JOB, re.DOTALL).group(1)
        self.env.update(GITHUB_OUTPUT=str(self.root / "outputs"), GITHUB_ENV=str(self.root / "env"))
        with patch.dict(os.environ, self.env, clear=True), patch("os.statvfs") as stat, contextlib.redirect_stdout(io.StringIO()):
            stat.return_value.f_bavail = available
            stat.return_value.f_frsize = 1
            exec(compile(textwrap.dedent(script), "workflow-bootstrap", "exec"), {})
        return Path((self.root / "outputs").read_text().strip().split("=", 1)[1])

    def test_bootstrap_real_unique_private_child_survives_checkout_and_python_writes(self):
        scratch = self.bootstrap()
        second = self.bootstrap()
        self.assertNotEqual(scratch, second)
        self.assertEqual(0o700, scratch.stat().st_mode & 0o777)
        self.assertEqual(Path(self.env["RUNNER_TEMP"]), scratch.parent)
        checkout = Path(self.env["GITHUB_WORKSPACE"])
        checkout.rmdir()
        checkout.mkdir()
        env = dict(self.env, **{name: str(scratch) for name in ("TMPDIR", "TMP", "TEMP")})
        self.assertEqual(scratch, DISK["verify_temp"](env))

    def test_actual_bootstrap_shell_exports_and_retains_failure_log_with_nonzero_exit(self):
        script = JOB.split("        run: |\n", 1)[1].split("      - name: Checkout code", 1)[0]
        script = textwrap.dedent(script)
        commands = self.root / "commands"
        commands.mkdir()
        python = commands / "python3"
        # Inject only the disk capacity boundary; execute the workflow's actual Python and shell.
        python.write_text(f'#!{sys.executable}\nimport os, sys, types\n'
            'os.statvfs = lambda path: types.SimpleNamespace(f_bavail=int(os.environ["TEST_FREE_BYTES"]), f_frsize=1)\n'
            'exec(compile(sys.stdin.read(), "workflow-bootstrap", "exec"), {})\n')
        python.chmod(0o700)
        env = dict(self.env, PATH=str(commands) + os.pathsep + os.environ["PATH"],
                   GITHUB_OUTPUT=str(self.root / "shell-output"), GITHUB_ENV=str(self.root / "shell-env"))
        log = Path(env["RUNNER_TEMP"]) / "stage342-bootstrap-123-2.log"
        for free, expected_exit in ((80 * GIB, 0), (36 * GIB, 1)):
            result = subprocess.run(["bash", "--noprofile", "--norc", "-c", script],
                                    env=dict(env, TEST_FREE_BYTES=str(free)), capture_output=True, text=True, timeout=10)
            self.assertEqual(expected_exit, result.returncode, result.stdout + result.stderr)
            self.assertTrue(log.read_text())
            if free < 37 * GIB:
                self.assertIn("needs >=37 GiB", log.read_text())
            else:
                self.assertIn("Python temp resolution/write passed", log.read_text())
        exports = (self.root / "shell-env").read_text().splitlines()
        self.assertEqual({"TMPDIR", "TMP", "TEMP"}, {line.split("=", 1)[0] for line in exports})
        scratch = Path(exports[0].split("=", 1)[1])
        self.assertTrue(scratch.is_dir())
        self.assertTrue((self.root / "shell-output").is_file())

    def test_checked_command_failures_timeouts_and_overlarge_output_fail_closed(self):
        for error in (subprocess.TimeoutExpired(["probe"], 10, output=b"partial proof", stderr=b"failed write"),):
            with patch("subprocess.run", side_effect=error), self.assertRaisesRegex(RuntimeError, "partial proof"):
                DISK["checked"](["probe"])
        result = subprocess.CompletedProcess(["probe"], 1, "partial proof", "failed write")
        with patch("subprocess.run", return_value=result), self.assertRaisesRegex(RuntimeError, "partial proof"):
            DISK["checked"](["probe"])
        result = subprocess.CompletedProcess(["probe"], 0, "x" * 65537, "")
        with patch("subprocess.run", return_value=result), self.assertRaisesRegex(RuntimeError, "diagnostic bound"):
            DISK["checked"](["probe"])

    def test_bootstrap_low_runner_and_checkout_nested_runner_fail_before_mktemp(self):
        with self.assertRaisesRegex(RuntimeError, "37 GiB"):
            self.bootstrap(36 * GIB)
        self.assertFalse((self.root / "outputs").exists())
        self.env["RUNNER_TEMP"] = self.env["GITHUB_WORKSPACE"]
        with self.assertRaisesRegex(RuntimeError, "outside the checkout"):
            self.bootstrap()

    def test_python_resolver_and_write_failures_are_not_silently_accepted(self):
        with patch.dict(DISK["verify_temp"].__globals__, {"checked": lambda *a, **k: "/tmp"}), \
                self.assertRaisesRegex(RuntimeError, "escaped"):
            DISK["verify_temp"](self.env)
        with patch.dict(DISK["verify_temp"].__globals__, {"checked": lambda *a, **k: (_ for _ in ()).throw(OSError("write failed"))}), \
                self.assertRaisesRegex(OSError, "write failed"):
            DISK["verify_temp"](self.env)

    def test_nuget_resolves_all_default_or_override_paths_and_refuses_missing_or_relative(self):
        for base in (self.root / "defaults", self.root / "overrides"):
            output = "\n".join(f"{name}: {base / name}" for name in ("global-packages", "http-cache", "temp", "plugins-cache"))
            with patch.dict(DISK["nuget_locations"].__globals__, {"checked": lambda command: output}):
                self.assertEqual(4, len(DISK["nuget_locations"]()))
        for output in ("", "global-packages: relative", "global-packages: /a\nglobal-packages: /b"):
            with patch.dict(DISK["nuget_locations"].__globals__, {"checked": lambda command: output}), self.assertRaises(RuntimeError):
                DISK["nuget_locations"]()

    def test_container_mount_or_symlink_mapping_change_is_rejected_even_on_same_filesystem(self):
        paths = DISK["container_locations"](self.kernel)
        guard = self.guard({"kernel": paths})
        self.kernel["Mounts"][0]["Source"] = "/changed"
        with patch.dict(guard.measure.__globals__, {"checked": self.stat}), self.assertRaisesRegex(RuntimeError, "mapping changed"):
            guard.check()
        guard = self.guard({"runner": ["/a"]})
        def remap(command):
            return "/new-target" if "realpath" in command else self.stat(command)
        with patch.dict(guard.measure.__globals__, {"checked": remap}), self.assertRaisesRegex(RuntimeError, "mapping changed"):
            guard.check()

    def test_container_mount_reordering_between_inspects_is_tolerated(self):
        # docker inspect does not guarantee Mounts order across calls on the same
        # live container; a bare reorder of the same mounts must not look like a
        # changed mapping.
        paths = DISK["container_locations"](self.kernel)
        reversed_mounts = list(reversed(self.kernel["Mounts"]))

        def reordering_stat(command):
            if "inspect" in command:
                return json.dumps([dict(self.kernel, Mounts=reversed_mounts)])
            return self.stat(command)

        with patch.dict(DISK["DiskGuard"].measure.__globals__, {"checked": reordering_stat}):
            guard = DISK["DiskGuard"]({"kernel": paths}, "kernel", self.root)
            guard.check()

    def test_unknown_container_temp_and_invalid_capacity_fail_closed(self):
        self.kernel["Config"]["Env"] = ["TMPDIR=relative"]
        with self.assertRaisesRegex(RuntimeError, "Unknown"):
            DISK["container_locations"](self.kernel)
        for fsid in ("0", "not-a-number"):
            self.fsid = fsid
            with self.assertRaises((RuntimeError, ValueError)):
                self.guard({"runner": ["/a"]})

    def test_cleanup_refuses_nonempty_failed_unowned_and_symlink_scratch(self):
        self.env["JOB_STATUS"] = "success"
        proof = self.scratch / "live-proof.txt"
        proof.write_text("keep")
        DISK["cleanup"](self.env)
        self.assertEqual("keep", proof.read_text())
        proof.unlink()
        self.env["JOB_STATUS"] = "failure"
        DISK["cleanup"](self.env)
        self.assertTrue(self.scratch.is_dir())
        self.env["JOB_STATUS"] = "success"
        marker = self.scratch / ".stage342-owner"
        marker.write_text("not-owned")
        with self.assertRaisesRegex(RuntimeError, "ownership"):
            DISK["cleanup"](self.env)
        marker.write_text("123:2")
        link = self.scratch.parent / "stage342-tmp.link"
        link.symlink_to(self.scratch, target_is_directory=True)
        with self.assertRaisesRegex(RuntimeError, "private"):
            DISK["cleanup"](dict(self.env, TMPDIR=str(link)))
        DISK["cleanup"](self.env)
        self.assertFalse(self.scratch.exists())

    def test_dotnet_probe_executes_real_source_and_rejects_bad_resolver_or_write(self):
        actual_checked = DISK["checked"]
        caches = {name: str(self.root / name) for name in ("global-packages", "http-cache", "temp", "plugins-cache")}
        for mode in ("pass", "escape", "failure"):
            project = self.scratch / "dotnet-probe"
            if project.exists():
                # Remove only the two known tiny test-owned source files, never real build output.
                for name in ("Probe.csproj", "Program.cs"):
                    (project / name).unlink()
                project.rmdir()
            def execute(command, **kwargs):
                if command[:2] == ["dotnet", "run"]:
                    source = (project / "Program.cs").read_text()
                    self.assertIn("Path.GetTempPath()", source)
                    self.assertIn("FileMode.CreateNew", source)
                    self.assertIn("file.Flush(true)", source)
                    self.assertEqual(self.env, kwargs["env"])
                    if mode == "failure":
                        raise RuntimeError(".NET write failed")
                    Path(command[-1]).write_text(str(self.scratch if mode == "pass" else self.root))
                    return "dotnet probe output"
                if command[0] == "dotnet":
                    return "\n".join(f"{name}: {path}" for name, path in caches.items())
                if command[0] in ("stat", "realpath"):
                    return self.stat(command)
                return actual_checked(command, **kwargs)
            with patch.dict(DISK["probe_dotnet"].__globals__, {"checked": execute}):
                if mode == "pass":
                    DISK["probe_dotnet"](self.env)
                    self.assertTrue((self.scratch / "nuget-paths.json").is_file())
                    DISK["runtime_locations"](self.env)
                    (self.scratch / "nuget-paths.json").write_text("{}")
                    with self.assertRaisesRegex(RuntimeError, "mappings changed"):
                        DISK["runtime_locations"](self.env)
                else:
                    with self.assertRaisesRegex(RuntimeError, "escaped|write failed"):
                        DISK["probe_dotnet"](self.env)
                self.assertTrue((self.scratch / "dotnet-probe.log").is_file())

    def test_bootstrap_precedes_checkout_setup_and_cache_and_exports_all_temp_variables(self):
        self.assertLess(JOB.index("Bootstrap private runner scratch"), JOB.index("uses: actions/checkout"))
        self.assertLess(JOB.index("Verify scratch survived checkout"), JOB.index("uses: actions/setup-dotnet"))
        self.assertLess(JOB.index("Probe .NET temp and NuGet locations"), JOB.index("uses: Cratis/Workflows"))
        for name in ("TMPDIR", "TMP", "TEMP"):
            self.assertIn(f'export {name}=', JOB)
            self.assertIn(f'{name}=', JOB.split("GITHUB_ENV", 1)[0])
        self.assertIn("Upload bootstrap disk diagnostics", JOB)
        self.assertIn("if: always()", JOB.split("Upload bootstrap disk diagnostics", 1)[1])

    def test_live_proofs_deadlines_and_nonrecursive_generated_cleanup_are_retained(self):
        self.assertIn('"total": 2, "executed": 2, "passed": 2', JOB)
        self.assertIn('len(results) != len(expected)', JOB)
        self.assertIn('"600s", "dotnet", "test"', JOB)
        self.assertIn("os.killpg(process.pid, signal.SIGKILL)", JOB)
        self.assertIn('output.rmdir()', JOB)
        self.assertIn('rmdir -- "$STAGE342_OUTPUT"', JOB)
        self.assertNotRegex(JOB, r"rm\s+-[rf]+")


if __name__ == "__main__":
    unittest.main()
