#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Keep routing tools outside the workspace and select only trusted revisions."""

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
DECISION_COMMANDS = {"routing": "route", "dotnet-build": "write-slnf", "analyzers": "write-slnf",
                     "build": "check-build", "routed-checks": "check"}
DECISION_JOBS = tuple(DECISION_COMMANDS)
PIN = "3d3c42e5aac5ba805825da76410c181273ba90b1"


def step(job, name):
    return JOBS[job].split(f"      - name: {name}\n", 1)[1].split("\n      - name:", 1)[0]


def shell(job, name):
    block = step(job, name)
    if "run: *select-routing-tools" in block:
        block = step("routing", "Select routing tools")
    return textwrap.dedent(re.split(r"        run: (?:&[\w-]+ )?\|\n", block, maxsplit=1)[1]).rstrip()


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

    def test_decisions_share_trusted_selection_and_isolated_python(self):
        for job, command in DECISION_COMMANDS.items():
            with self.subTest(job=job):
                selection = step(job, "Select routing tools")
                self.assertIn("CI_BASE_SHA: ${{ github.event.pull_request.base.sha }}", selection)
                self.assertIn("CI_DEFAULT_BRANCH: ${{ github.event.repository.default_branch }}", selection)
                self.assertIn(f"CI_TOOLS_COMMAND: {command}", selection)
                self.assertEqual(shell("routing", "Select routing tools"), shell(job, "Select routing tools"))
                self.assertNotIn("${{", shell(job, "Select routing tools"))
                before_selection = JOBS[job].split("      - name: Select routing tools", 1)[0]
                self.assertNotIn("run:", before_selection)
                self.assertIn(f'python3 -I "$CI_ROUTING_SCRIPT" {command}', JOBS[job])
        self.assertIn("fetch-depth: 0", step("routing", "Checkout merge-base history"))
        self.assertNotIn("working-directory:", JOBS["routing"])
        self.assertIn(f"actions/checkout@{PIN}", step("build", "Checkout code"))

    def test_no_checkout_places_routing_tools_in_the_workspace(self):
        for job in DECISION_JOBS:
            checkouts = [item for item in JOBS[job].split("      - name:") if "uses: actions/checkout@" in item]
            with self.subTest(job=job):
                self.assertEqual(1, len(checkouts))
                self.assertNotRegex(checkouts[0], r"\n\s+path:")
                self.assertLess(JOBS[job].index("Select routing tools"),
                                JOBS[job].index('python3 -I "$CI_ROUTING_SCRIPT"'))

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
        temporary = tempfile.TemporaryDirectory(dir=ROOT / ".ai-work")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.directory = self.root / "workspace"
        self.runner_temp = self.root / "runner-temp"
        self.environment_file = self.root / "environment"
        self.directory.mkdir()
        self.runner_temp.mkdir()
        self.git("init", "--initial-branch=main")
        self.git("init", "--bare", "--initial-branch=main", str(self.root / "origin"))
        self.git("remote", "add", "origin", str(self.root / "origin"))
        self.empty_base = self.publish("Initial policy")

    def git(self, *arguments):
        result = subprocess.run(["git", "-c", "user.name=CI specs", "-c", "user.email=ci@example.invalid",
                                 "-c", "core.hooksPath=/dev/null", *arguments], cwd=self.directory,
                                env={**os.environ, "GIT_CONFIG_GLOBAL": os.devnull, "GIT_CONFIG_NOSYSTEM": "1"},
                                text=True, capture_output=True, check=False)
        self.assertEqual(0, result.returncode, result.stderr)
        return result.stdout.strip()

    def commit(self, message):
        self.git("add", "--all")
        self.git("commit", "--allow-empty", "-m", message)
        return self.git("rev-parse", "HEAD")

    def publish(self, message):
        revision = self.commit(message)
        self.git("push", "origin", "HEAD:refs/heads/main")
        return revision

    def publish_tools(self, policy, commands=tuple(DECISION_COMMANDS.values())):
        tools = self.directory / ".github"
        tools.mkdir(exist_ok=True)
        (tools / "ci-routing.py").write_text(
            "from pathlib import Path\n"
            f"COMMANDS = {json.dumps(commands)}\n"
            "print((Path(__file__).resolve().parent.parent / 'policy.txt').read_text().strip())\n")
        (tools / "spec-shards.py").write_text("SHARDS = 'trusted'\n")
        (self.directory / "Stage.slnx").write_text("<Solution />")
        (self.directory / "policy.txt").write_text(policy)
        return self.publish(policy)

    def create_head(self):
        tools = self.directory / ".github"
        tools.mkdir(exist_ok=True)
        (tools / "ci-routing.py").write_text(
            "from pathlib import Path\nPath('head-executed').touch()\nraise SystemExit(7)\n")
        (self.directory / "policy.txt").write_text("head policy")
        (self.directory / "gate-tools").symlink_to(".")
        return self.commit("Head inputs")

    def select(self, job, base, event="pull_request"):
        self.environment_file.write_text("")
        return run_shell(shell(job, "Select routing tools"), self.directory,
                         GITHUB_EVENT_NAME=event, GITHUB_ENV=str(self.environment_file),
                         GITHUB_WORKSPACE=str(self.directory), RUNNER_TEMP=str(self.runner_temp),
                         CI_BASE_SHA=base, CI_DEFAULT_BRANCH="main", CI_TOOLS_COMMAND=DECISION_COMMANDS[job])

    def selected_script(self, job, base, revision, event="pull_request"):
        result = self.select(job, base, event)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn(f"CI routing tools revision: {revision}", result.stdout)
        selected = Path(self.environment_file.read_text().strip().split("=", 1)[1]).resolve()
        self.assertTrue(selected.is_relative_to(self.runner_temp))
        self.assertFalse(selected.is_relative_to(self.directory))
        self.assertFalse((self.directory / "head-executed").exists())
        return selected

    def assert_policy(self, base, revision, policy, event="pull_request"):
        for job in DECISION_JOBS:
            with self.subTest(job=job, event=event):
                script = self.selected_script(job, base, revision, event)
                result = subprocess.run([sys.executable, "-I", str(script), DECISION_COMMANDS[job]],
                                        cwd=self.directory, text=True, capture_output=True, check=False)
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual(policy, result.stdout.strip())
                self.assertFalse((self.directory / "head-executed").exists())

    def assert_closed(self, base, missing, jobs=DECISION_JOBS):
        for job in jobs:
            with self.subTest(job=job):
                result = self.select(job, base)
                self.assertNotEqual(0, result.returncode)
                self.assertIn(f"::error::Trusted routing tools are missing {missing}", result.stdout)
                self.assertEqual("", self.environment_file.read_text())
                self.assertFalse((self.directory / "head-executed").exists())

    def test_head_symlink_does_not_change_base_tools_or_policy(self):
        base = self.publish_tools("base policy")
        self.publish_tools("default policy")
        self.create_head()
        self.assert_policy(base, base, "base policy")

    def test_base_objects_not_in_checkout_are_fetched_by_sha(self):
        base = self.publish_tools("base policy")
        self.create_head()
        self.git("push", "origin", "HEAD:refs/heads/pull-inputs")
        shallow = self.root / "shallow"
        self.git("clone", "--depth=1", "--branch=pull-inputs", (self.root / "origin").as_uri(), str(shallow))
        self.directory = shallow
        self.assert_policy(base, base, "base policy")

    def test_default_branch_current_head_is_resolved_from_origin(self):
        self.publish_tools("initial default policy")
        self.create_head()
        workspace = self.directory
        publisher = self.root / "publisher"
        self.git("clone", str(self.root / "origin"), str(publisher))
        self.directory = publisher
        default = self.publish_tools("current default policy")
        self.directory = workspace
        self.assert_policy(self.empty_base, default, "current default policy")

    def test_base_without_script_uses_default_branch_tools(self):
        default = self.publish_tools("default policy")
        self.create_head()
        self.assert_policy(self.empty_base, default, "default policy")

    def test_neither_trusted_revision_has_script_fails_closed(self):
        self.create_head()
        self.assert_closed(self.empty_base, ".github/ci-routing.py")

    def test_non_pr_events_use_default_branch_not_checkout_revision(self):
        default = self.publish_tools("default policy")
        head = self.create_head()
        for event in ("schedule", "workflow_dispatch"):
            self.assert_policy(head, default, "default policy", event)

    def test_missing_required_command_uses_default_branch(self):
        base = self.publish_tools("base policy", commands=("route",))
        default = self.publish_tools("default policy")
        self.create_head()
        self.selected_script("dotnet-build", base, default)

    def test_neither_revision_has_required_command_fails_closed(self):
        base = self.publish_tools("base policy", commands=("route",))
        self.create_head()
        self.assert_closed(base, ".github/ci-routing.py command write-slnf", jobs=("dotnet-build", "analyzers"))

    def test_missing_policy_tool_uses_default_branch(self):
        self.publish_tools("base policy")
        (self.directory / ".github/spec-shards.py").unlink()
        base = self.commit("Policy tool not present")
        default = self.publish_tools("default policy")
        self.create_head()
        self.assert_policy(base, default, "default policy")

    def test_base_execution_failure_does_not_select_another_revision(self):
        self.publish_tools("base policy")
        with (self.directory / ".github/ci-routing.py").open("a") as script:
            script.write("raise SystemExit(7)\n")
        base = self.commit("Tool execution result")
        self.publish_tools("default policy")
        self.create_head()
        selected = self.selected_script("routing", base, base)
        result = subprocess.run([sys.executable, "-I", str(selected), "route"], cwd=self.directory, check=False)
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
