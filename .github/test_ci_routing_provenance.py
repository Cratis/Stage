#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Keep routing decisions and policy on base, with PR history and build inputs on head."""

import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import textwrap
import unittest

ROOT = Path(__file__).resolve().parent.parent
WORKFLOWS = ROOT / ".github/workflows"
WORKFLOW = (WORKFLOWS / "dotnet-build.yml").read_text()
SECTIONS = re.split(r"^  ([\w-]+):\n", WORKFLOW.split("jobs:\n", 1)[1], flags=re.MULTILINE)
JOBS = dict(zip(SECTIONS[1::2], SECTIONS[2::2]))
DECISION_JOBS = ("routing", "dotnet-build", "analyzers", "build", "routed-checks")
PIN = "3d3c42e5aac5ba805825da76410c181273ba90b1"


def step(job, name):
    return JOBS[job].split(f"      - name: {name}\n", 1)[1].split("\n      - name:", 1)[0]


def shell(job, name):
    return textwrap.dedent(step(job, name).split("        run: |\n", 1)[1]).rstrip()


def run_shell(script, directory, **environment):
    return subprocess.run(["bash", "-e", "-o", "pipefail", "-c", script], cwd=directory,
                          env={**os.environ, **environment}, text=True, capture_output=True, check=False)


class WorkflowProvenanceTests(unittest.TestCase):
    def test_every_checkout_disables_credential_persistence(self):
        for workflow in WORKFLOWS.glob("*.yml"):
            steps = re.split(r"^      - ", workflow.read_text(), flags=re.MULTILINE)
            for checkout in (item for item in steps if "uses: actions/checkout@" in item):
                with self.subTest(workflow=workflow.name, step=checkout.splitlines()[0]):
                    self.assertIn("persist-credentials: false", checkout)

    def test_decisions_use_pinned_base_checkout_and_isolated_python(self):
        for job in DECISION_JOBS:
            with self.subTest(job=job):
                checkout = step(job, "Checkout routing tools")
                self.assertIn("if: github.event_name == 'pull_request'", checkout)
                self.assertIn(f"uses: actions/checkout@{PIN}", checkout)
                self.assertIn("ref: ${{ github.event.pull_request.base.sha }}", checkout)
                self.assertIn("path: gate-tools", checkout)
                self.assertIn('python3 -I "$CI_ROUTING_SCRIPT"', JOBS[job])
                self.assertLess(JOBS[job].index("Select routing tools"),
                                JOBS[job].index('python3 -I "$CI_ROUTING_SCRIPT"'))
        self.assertIn('"$CI_ROUTING_SCRIPT" route', JOBS["routing"])
        self.assertIn('"$CI_ROUTING_SCRIPT" check-build', JOBS["build"])
        self.assertIn('"$CI_ROUTING_SCRIPT" check', JOBS["routed-checks"])
        self.assertIn("fetch-depth: 0", step("routing", "Checkout merge-base history"))
        self.assertNotIn("working-directory:", JOBS["routing"])
        self.assertIn(f"actions/checkout@{PIN}", step("build", "Checkout code"))

    def test_filter_selection_precedes_pr_build_tools(self):
        for job in ("dotnet-build", "analyzers"):
            with self.subTest(job=job):
                self.assertLess(JOBS[job].index("Write affected solution filter"), JOBS[job].index("Setup .Net"))
                self.assertIn("if: needs.routing.outputs.build-scope == 'affected'",
                              step(job, "Write affected solution filter"))
                self.assertNotIn("write-slnf", step(job, "Build Debug" if job == "dotnet-build"
                                                   else "Build Debug with analyzers"))


class RoutingToolSelectionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.directory = Path(temporary.name)

    def create_base_tools(self):
        tools = self.directory / "gate-tools/.github"
        tools.mkdir(parents=True)
        (tools / "ci-routing.py").write_text("raise SystemExit(7)\n")

    def select(self, job, event):
        environment_file = self.directory / "environment"
        environment_file.write_text("")
        # Only revision logging is stubbed; execute the workflow's selection verbatim.
        script = 'git() { if [ "$1" = -C ]; then echo base-revision; else echo head-revision; fi; }\n'
        result = run_shell(script + shell(job, "Select routing tools"), self.directory,
                           GITHUB_EVENT_NAME=event, GITHUB_ENV=str(environment_file))
        self.assertEqual(0, result.returncode, result.stderr)
        return environment_file.read_text().strip().split("=", 1)[1], result.stdout

    def assert_event_selection(self, event, expected_script, revision):
        for job in DECISION_JOBS:
            with self.subTest(job=job, event=event):
                selected, output = self.select(job, event)
                self.assertEqual(expected_script, selected)
                self.assertIn(revision, output)
                self.assertNotIn("Bootstrap", output)

    def test_pull_requests_select_base_tools(self):
        self.create_base_tools()
        self.assert_event_selection("pull_request", "gate-tools/.github/ci-routing.py", "base-revision")

    def test_missing_base_script_bootstraps_with_logged_head_revision(self):
        for job in DECISION_JOBS:
            with self.subTest(job=job):
                selected, output = self.select(job, "pull_request")
                self.assertEqual(".github/ci-routing.py", selected)
                self.assertIn("Bootstrap: base has no ci-routing.py", output)
                self.assertIn("head-revision", output)

    def test_non_pr_events_keep_head_tools_even_when_base_exists(self):
        self.create_base_tools()
        for event in ("schedule", "workflow_dispatch"):
            self.assert_event_selection(event, ".github/ci-routing.py", "head-revision")

    def test_base_execution_failure_does_not_select_head(self):
        self.create_base_tools()
        selected, _ = self.select("routing", "pull_request")
        result = subprocess.run([sys.executable, "-I", selected, "route"], cwd=self.directory, check=False)
        self.assertEqual(7, result.returncode)


class BasePolicyTests(unittest.TestCase):
    def test_policy_is_relative_to_script_not_pr_working_directory(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            (directory / ".github").mkdir()
            (directory / ".github/ci-routing.py").write_text("raise SystemExit(7)\n")
            (directory / ".github/spec-shards.py").write_text("raise SystemExit(7)\n")
            (directory / "Stage.slnx").write_text("not a solution")
            output = directory / "routes"
            event = directory / "event.json"
            event.write_text("{}")
            result = subprocess.run([sys.executable, "-I", str(ROOT / ".github/ci-routing.py"), "route"],
                                    cwd=directory, env={**os.environ, "GITHUB_EVENT_NAME": "schedule",
                                                       "GITHUB_EVENT_PATH": str(event), "GITHUB_OUTPUT": str(output)},
                                    text=True, capture_output=True, check=False)
            self.assertEqual(0, result.returncode, result.stderr)
            routes = dict(line.split("=", 1) for line in output.read_text().splitlines())
            self.assertEqual("full", routes["build-scope"])
            self.assertIn("Source/Host/Host.csproj", json.loads(routes["build-projects"]))
            self.assertTrue(json.loads(routes["spec-shards"]))

    def test_base_filter_keeps_selected_projects_but_targets_head_solution(self):
        for job in ("dotnet-build", "analyzers"):
            with self.subTest(job=job), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary)
                result = run_shell(shell(job, "Write affected solution filter"), directory,
                                   CI_ROUTING_SCRIPT=str(ROOT / ".github/ci-routing.py"),
                                   BUILD_PROJECTS='["Source/Stage/Stage.csproj"]',
                                   GITHUB_WORKSPACE=str(directory), RUNNER_TEMP=str(directory))
                self.assertEqual(0, result.returncode, result.stderr)
                selected = json.loads((directory / "affected.slnf").read_text())["solution"]
                self.assertEqual(str(directory / "Stage.slnx"), selected["path"])
                self.assertEqual(["Source/Stage/Stage.csproj"], selected["projects"])

    def test_head_only_project_cannot_change_base_filter_validation(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            (directory / "Stage.slnx").write_text('<Solution><Project Path="HeadOnly.csproj" /></Solution>')
            result = run_shell(shell("dotnet-build", "Write affected solution filter"), directory,
                               CI_ROUTING_SCRIPT=str(ROOT / ".github/ci-routing.py"),
                               BUILD_PROJECTS='["HeadOnly.csproj"]',
                               GITHUB_WORKSPACE=str(directory), RUNNER_TEMP=str(directory))
            self.assertNotEqual(0, result.returncode)
            self.assertIn("Solution filter needs solution projects", result.stderr)


if __name__ == "__main__":
    unittest.main()
