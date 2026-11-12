#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Exercise the real blueprint shell scheduler with fake containers and browsers."""

import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parent.parent
BLUEPRINTS = ROOT / "Verification/live-blueprints"
MODELS = [entry["model"] for entry in json.loads((BLUEPRINTS / "blueprints.json").read_text())]

# Each command writes one atomic event. No Docker, npm registry or browser is used.
FAKE_TOOL = r'''#!/usr/bin/env python3
import json
import os
from pathlib import Path
import re
import sys
import time

root = Path(os.environ["FAKE_ROOT"])
args = sys.argv[1:]
tool = Path(sys.argv[0]).name

def event(kind, **fields):
    with (root / "events").open("a") as stream:
        stream.write(json.dumps(dict(kind=kind, **fields)) + "\n")

def node():
    if args[0] == "-pe":
        print("ready")
        return 0
    if args[0] == "-e":
        entries = json.loads((root / "Verification/live-blueprints/blueprints.json").read_text())
        match = re.search(r"\[(\d+)\]", args[1])
        print(entries[int(match[1])]["model"] if match else len(entries))
        return 0
    if args[0].endswith("prepare.cjs"):
        event("prepare", index=int(args[2]), modules=args[1], packages=args[3])
        if args[2] == os.environ.get("FAIL_PREPARE"):
            return 1
        Path(args[3]).mkdir(parents=True)
        (Path(args[3]) / "approved-packages.json").write_text("{}")
        return 0
    if args[0].endswith("scenario.cjs"):
        return scenario()
    raise RuntimeError(args)

def wait_for_batch(model):
    entries = json.loads((root / "Verification/live-blueprints/blueprints.json").read_text())
    index = [entry["model"] for entry in entries].index(model)
    workers = int(os.environ.get("STAGE_LIVE_CONCURRENCY") or "1")
    first = index // workers * workers
    expected = set(range(first, min(first + workers, len(entries))))
    expected.discard(int(os.environ.get("FAIL_PREPARE", "-1")))
    deadline = time.monotonic() + 5
    while time.monotonic() < deadline:
        events = [json.loads(line) for line in (root / "events").read_text().splitlines()]
        started = {int(event["container"].rsplit("-", 1)[1]) for event in events if event["kind"] == "start"}
        if expected.issubset(started):
            return
        time.sleep(0.01)
    raise RuntimeError("the scheduler did not launch the full batch")

def scenario():
    model = os.environ["STAGE_LIVE_NAME"]
    event("scenario", model=model, url=os.environ["STAGE_LIVE_URL"],
          approved=os.environ["STAGE_LIVE_APPROVED"], model_path=os.environ["STAGE_LIVE_MODEL"],
          result=os.environ["STAGE_LIVE_RESULT"])
    wait_for_batch(model)
    code = int(os.environ.get("FAIL_CODE", "1")) if model == os.environ.get("FAIL_MODEL") else 0
    Path(os.environ["STAGE_LIVE_RESULT"]).write_text(json.dumps({"status": "passed" if code == 0 else "failed"}))
    print(f"verified {model}")
    return code

def docker():
    if args[0] == "run":
        name = args[args.index("--name") + 1]
        event("start", container=name, port=args[args.index("--publish") + 1],
              volumes=[args[i + 1] for i, arg in enumerate(args) if arg == "--volume"])
        return int(name.endswith("-" + os.environ.get("FAIL_START", "none")))
    if args[0] == "rm":
        event("remove", container=args[-1])
    if args[0] == "logs":
        print("Stage log for " + args[-1])
    return 0

if tool == "node":
    sys.exit(node())
if tool == "docker":
    sys.exit(docker())
if tool == "curl":
    print('{"state":"ready"}')
elif tool == "npm":
    event("install")
    (Path.cwd() / "node_modules").mkdir()
else:
    raise RuntimeError(tool)
'''


class LiveBlueprintTests(unittest.TestCase):
    def setUp(self):
        scratch = ROOT / ".ai-work/live/tests"
        scratch.mkdir(parents=True, exist_ok=True)
        task = tempfile.TemporaryDirectory(dir=scratch)
        self.addCleanup(task.cleanup)
        self.root = Path(task.name)
        self.here = self.root / "Verification/live-blueprints"
        self.here.mkdir(parents=True)
        for name in ("run.sh", "package.json", "blueprints.json"):
            shutil.copy2(BLUEPRINTS / name, self.here / name)
        (self.root / "Verification/generated-react-browser/node_modules/playwright").mkdir(parents=True)
        tools = self.root / "bin"
        tools.mkdir()
        command = tools / "fake"
        command.write_text(FAKE_TOOL)
        command.chmod(0o755)
        for name in ("node", "npm", "docker", "curl"):
            (tools / name).symlink_to(command)
        self.env = dict(os.environ, PATH=f"{tools}:{os.environ['PATH']}", FAKE_ROOT=str(self.root))
        self.results = self.root / "results"

    def run_models(self, workers="3", **overrides):
        env = dict(self.env, STAGE_LIVE_CONCURRENCY=workers, **overrides)
        run = subprocess.run(["bash", str(self.here / "run.sh"), "stage:test", str(self.results)],
                             env=env, text=True, capture_output=True, timeout=45)
        events = self.root / "events"
        self.events = [json.loads(line) for line in events.read_text().splitlines()] if events.exists() else []
        return run

    def of_kind(self, kind):
        return [event for event in self.events if event["kind"] == kind]

    def assert_all_verified(self):
        scenarios = self.of_kind("scenario")
        self.assertCountEqual(MODELS, [event["model"] for event in scenarios])
        self.assertEqual(15, len(scenarios))
        for event in scenarios:
            model = event["model"]
            folder = self.results / model
            self.assertEqual(str(folder / f"live-blueprints-{model}.json"), event["result"])
            self.assertTrue(Path(event["result"]).is_file())
            self.assertIn(f"verified {model}", (folder / "scenario.log").read_text())
            self.assertIn("Stage log", (folder / "stage.log").read_text())
            self.assertEqual(str(self.here / f"models/{model}/{model}.play"), event["model_path"])
            self.assertFalse(Path(event["approved"]).is_file())  # temporary packages are cleaned up

    def assert_bound(self, limit):
        active = set()
        peak = 0
        for event in self.events:
            if event["kind"] == "start":
                active.add(event["container"])
                peak = max(peak, len(active))
            if event["kind"] == "remove":
                active.discard(event["container"])
            self.assertLessEqual(len(active), limit)
        self.assertFalse(active)
        self.assertEqual(limit, peak)

    def test_all_fifteen_run_with_three_isolated_workers_and_retained_logs(self):
        run = self.run_models(STAGE_LIVE_PORT="21000")
        self.assertEqual(0, run.returncode, run.stdout + run.stderr)
        self.assert_all_verified()
        self.assert_bound(3)
        starts = self.of_kind("start")
        self.assertEqual(15, len({event["container"] for event in starts}))
        self.assertEqual({f"127.0.0.1:{21000 + i}:9090" for i in range(15)}, {event["port"] for event in starts})
        self.assertEqual(15, len({event["packages"] for event in self.of_kind("prepare")}))
        self.assertEqual(1, len(self.of_kind("install")))
        self.assertEqual(1, len({event["modules"] for event in self.of_kind("prepare")}))
        for model in MODELS:
            self.assertIn(f"verified {model}", run.stdout)

    def test_two_workers_and_sequential_default(self):
        for workers in ("2", ""):
            with self.subTest(workers=workers):
                run = self.run_models(workers)
                self.assertEqual(0, run.returncode, run.stdout + run.stderr)
                self.assert_all_verified()
                self.assert_bound(int(workers or "1"))
                (self.root / "events").unlink()

    def test_browser_failure_fails_step_without_skipping_any_model(self):
        for code in ("1", "2", "7"):
            with self.subTest(code=code):
                run = self.run_models(FAIL_MODEL="crm", FAIL_CODE=code)
                self.assertEqual(2 if code == "2" else 1, run.returncode, run.stdout + run.stderr)
                self.assert_all_verified()
                self.assert_bound(3)
                (self.root / "events").unlink()

    def test_prepare_or_container_start_failure_does_not_skip_other_models(self):
        for failure in ("FAIL_PREPARE", "FAIL_START"):
            with self.subTest(failure=failure):
                run = self.run_models(**{failure: "0"})
                self.assertEqual(2, run.returncode, run.stdout + run.stderr)
                self.assertEqual(15, len(self.of_kind("prepare")))
                self.assertCountEqual(MODELS[1:], [event["model"] for event in self.of_kind("scenario")])
                self.assertTrue((self.results / "crm/scenario.log").is_file())
                self.assertTrue((self.results / "crm/stage.log").is_file())
                (self.root / "events").unlink()

    def test_infrastructure_failure_takes_precedence_over_browser_failure(self):
        run = self.run_models(FAIL_PREPARE="0", FAIL_MODEL="ticketing", FAIL_CODE="1")
        self.assertEqual(2, run.returncode, run.stdout + run.stderr)
        self.assertEqual(15, len(self.of_kind("prepare")))
        self.assertCountEqual(MODELS[1:], [event["model"] for event in self.of_kind("scenario")])

    def test_empty_model_inventory_cannot_pass(self):
        (self.here / "blueprints.json").write_text("[]")
        run = self.run_models()
        self.assertEqual(2, run.returncode, run.stdout + run.stderr)
        self.assertEqual([], self.of_kind("scenario"))

    def test_invalid_parallelism_fails_before_install_or_launch(self):
        for workers in ("0", "4", "-1", "many"):
            with self.subTest(workers=workers):
                run = self.run_models(workers)
                self.assertEqual(2, run.returncode)
                self.assertEqual([], self.events)

    def test_workflow_only_opts_blueprint_step_into_three_workers(self):
        workflow = (ROOT / ".github/workflows/dotnet-build.yml").read_text()
        job = workflow.split("  live-custom-packages:\n", 1)[1].split("  generated-react:\n", 1)[0]
        step = job.split("      - name: Run the live domain blueprint scenarios\n", 1)[1].split("      - name:", 1)[0]
        self.assertIn('STAGE_LIVE_CONCURRENCY: "3"', step)
        self.assertIn("id: blueprints", step)
        for output, step_id in (("custom", "custom"), ("navigation", "navigation"), ("blueprints", "blueprints"), ("templates", "templates")):
            self.assertIn(f"{output}-result: ${{{{ steps.{step_id}.outcome }}}}", job)
        self.assertIn("TestResults/live-blueprints", job.split("      - name: Upload results", 1)[1])


if __name__ == "__main__":
    unittest.main()
