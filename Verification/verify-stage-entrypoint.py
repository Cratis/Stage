#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Exercise the real container supervisor with fake kernel/host processes, without Docker."""

import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


ENTRYPOINT = Path(__file__).resolve().parents[1] / "Source/Host/entrypoint-stage.sh"


class Entrypoint(unittest.TestCase):
    def setUp(self):
        self.workspace = tempfile.TemporaryDirectory(prefix="stage-entrypoint-")
        self.addCleanup(self.workspace.cleanup)
        self.root = Path(self.workspace.name)
        for directory in ("app", "stage", "bin", "model"):
            (self.root / directory).mkdir()
        self.receipt = self.root / "host.jsonl"
        self.kernel = self.root / "kernel-started"
        self.environment = os.environ.copy()
        self.environment.pop("STAGE_WARM", None)
        self.environment.update({
            "PATH": f"{self.root / 'bin'}{os.pathsep}{os.environ['PATH']}",
            "STAGE_TEST_ROOT": str(self.root),
            "BASH_ENV": str(self.root / "bootstrap.sh"),
            "STAGE_TEST_RESTART": "false",
            "STAGE_TEST_EXIT": "0",
        })
        # Map only the container's fixed working directories; input selection uses the real filesystem.
        (self.root / "bootstrap.sh").write_text('''
cd() {
    case "${1:-}" in
        /app) builtin cd "$STAGE_TEST_ROOT/app" ;;
        /stage) builtin cd "$STAGE_TEST_ROOT/stage" ;;
        *) builtin cd "$@" ;;
    esac
}
''')
        self.executable(self.root / "app/Cratis.Chronicle.Server", '''#!/bin/sh
printf 'started\\n' >> "$STAGE_TEST_ROOT/kernel-started"
''')
        self.executable(self.root / "bin/nc", "#!/bin/sh\nexit 0\n")
        self.executable(self.root / "bin/dotnet", '''#!/usr/bin/env python3
import json
import os
from pathlib import Path
import sys
receipt = Path(os.environ["STAGE_TEST_ROOT"]) / "host.jsonl"
first = not receipt.exists()
with receipt.open("a") as output:
    output.write(json.dumps(sys.argv[1:]) + "\\n")
if first and os.environ["STAGE_TEST_RESTART"] == "true":
    sys.exit(42)
sys.exit(int(os.environ["STAGE_TEST_EXIT"]))
''')

    @staticmethod
    def executable(path, content):
        path.write_text(content)
        path.chmod(0o755)

    def run_stage(self, *arguments):
        return subprocess.run(
            ["bash", str(ENTRYPOINT), *arguments], cwd=self.root,
            env=self.environment, capture_output=True, text=True, timeout=10,
        )

    def calls(self):
        return [json.loads(line) for line in self.receipt.read_text().splitlines()]

    def test_selected_file_keeps_spaces_and_uppercase_extension(self):
        selected = self.root / "model/selected file.PLAY"
        selected.write_text("module Sales")
        result = self.run_stage("model/selected file.PLAY")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.calls(), [["Cratis.Stage.Host.dll", str(selected)]])
        self.assertTrue(self.kernel.exists())

    def test_selected_folder_finds_nested_uppercase_files(self):
        nested = self.root / "model/nested"
        nested.mkdir()
        (nested / "input.PLAY").write_text("module Sales")
        result = self.run_stage("model")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.calls(), [["Cratis.Stage.Host.dll", str(self.root / "model")]])

    def test_invalid_cold_inputs_do_not_start_either_process(self):
        (self.root / "input.txt").write_text("module Sales")
        for selected in ("missing.play", "input.txt", "model"):
            with self.subTest(selected=selected):
                result = self.run_stage(selected)
                self.assertEqual(result.returncode, 1)
                self.assertIn("ERROR:", result.stderr)
                self.assertFalse(self.kernel.exists())
                self.assertFalse(self.receipt.exists())

    def test_warm_environment_does_not_require_model_files(self):
        self.environment["STAGE_WARM"] = "true"
        result = self.run_stage("missing.play")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.calls(), [["Cratis.Stage.Host.dll", "--warm"]])

    def test_warm_handoff_restarts_only_stage_with_the_handoff_directory(self):
        self.environment["STAGE_TEST_RESTART"] = "true"
        result = self.run_stage("--warm")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.calls(), [
            ["Cratis.Stage.Host.dll", "--warm"],
            ["Cratis.Stage.Host.dll", "/eventmodel"],
        ])
        self.assertEqual(self.kernel.read_text().splitlines(), ["started"])

    def test_host_failure_is_not_reported_as_success(self):
        self.environment["STAGE_TEST_EXIT"] = "23"
        result = self.run_stage("--warm")
        self.assertEqual(result.returncode, 23)
        self.assertEqual(len(self.calls()), 1)


if __name__ == "__main__":
    unittest.main(verbosity=2)
