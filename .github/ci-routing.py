#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Select optional CI lanes, require their success, and report failed nightlies.

For a full PR run: add ci:full, then push or use "Re-run all jobs" on the dotnet-build workflow
("Re-run failed jobs" does not rerun routing, so it ignores the label).

Affected-only PR builds: a pull request builds only the projects its diff reaches (through
project_inputs, plus what the selected lanes consume with --no-build) and runs only the spec
shards covering them. Push to main, schedule, workflow_dispatch and ci:full build and test
everything. Shared build files, workflows, this script, disk-read spec inputs (Samples, Verification,
Source/Frontend), any unmapped path and any error fall back to the full build.
"""

import argparse
from fnmatch import fnmatchcase
import json
import os
from pathlib import Path
import posixpath
import re
import runpy
import subprocess
import time
from typing import NamedTuple
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
LINKED_ITEM = re.compile(r'<(?:ProjectReference|Compile|EmbeddedResource|Content|None)\s+Include="([^"]+)"')
PUBLISHED_PROJECT = re.compile(r'dotnet publish[^\n]*?((?:\.\./)+[\w./-]+\.csproj)')


def _relative(base, include):
    return posixpath.normpath(posixpath.join(base, include.replace("\\", "/").replace("$(MSBuildThisFileDirectory)", "")))


def project_inputs(project, own_folder=True, root=ROOT):
    """Every path a project builds from: referenced and published projects, plus linked files.

    Cross-folder <Compile/EmbeddedResource Include="../..."> links, ProjectReferences and nested
    `dotnet publish` targets are followed recursively, so a new link routes its lanes automatically.
    """
    patterns, seen, pending = set(), set(), [(project, own_folder)]
    while pending:
        current, include_folder = pending.pop()
        if current in seen:
            continue
        seen.add(current)
        folder = posixpath.dirname(current)
        if include_folder:
            patterns.add(f"{folder}/**")
        text = (root / current).read_text()
        includes = [item for match in LINKED_ITEM.findall(text) for item in match.split(";")]
        for include in includes + PUBLISHED_PROJECT.findall(text):
            path = _relative(folder, include.strip())
            if path.endswith(".csproj"):
                pending.append((path, True))
            elif not path.startswith(folder + "/"):
                patterns.add(path)
    return tuple(sorted(patterns))


def image_inputs(dockerfile="Source/Host/Dockerfile", root=ROOT):
    """Repository sources the Host image copies in (build-stage copies and publish output excluded)."""
    patterns = set()
    for line in (root / dockerfile).read_text().splitlines():
        words = line.split()
        if not words or words[0] != "COPY" or any(word.startswith("--from") for word in words):
            continue
        for source in (word for word in words[1:-1] if not word.startswith("--")):
            path = source.rstrip("/")
            if not (root / path).exists():
                continue  # Build output such as Source/Host/out; its inputs are the Host project's.
            patterns.add(f"{path}/**" if source.endswith("/") or (root / path).is_dir() else path)
    return tuple(sorted(patterns))


HOST = project_inputs("Source/Host/Host.csproj")
HOST_IMAGE = tuple(sorted(set(HOST + image_inputs())))
RENDERER = tuple(sorted(set(project_inputs("Source/Rendering.Cratis/Rendering.Cratis.csproj") + project_inputs(
    "Source/Rendering.Cratis.Scaffolding/Rendering.Cratis.Scaffolding.csproj"))))

SHARED = (
    "*.props", "*.targets", "Source/Directory.Build.*", "global.json", "*[Nn]u[Gg]et.[Cc]onfig", "*packages.lock.json",
    "Source/Contracts/**", "Source/Host/Dockerfile",
    "Verification/verify-runtime-compatibility.py", "Verification/synchronize-rendered-pins.py",
    ".github/workflows/**", ".github/*.py",
)
# Admission, emission and compilation transitively reach the whole renderer: routing
# subfolders individually risks silently omitting dependencies as the implementation evolves.
NAME_MATRIX = (
    "Source/Rendering.Cratis/**", "Source/Rendering.Cratis.Scaffolding/**",
    "Source/Specifications/**", "Source/**/for_ThreeWayConformance/**",
    "Source/Conformance.Specs/Conformance.Specs.csproj",
) + RENDERER + project_inputs("Source/Specifications/Specifications.csproj")
# There is no local Scene blueprint package: its implementation is pinned in the
# frontend manifests/scaffold, with the shipped catalog and registration below.
BLUEPRINT_SOURCES = (
    "Source/Frontend/**/blueprint.*", "Source/Frontend/package*.json",
    "Source/Frontend/Runtime/package*.json", "Source/Rendering.Cratis/Shipped/**",
    "Source/Rendering.Cratis/Scaffolding/CratisFrontendPackageSet.cs",
)
TEMPLATE_RENDERERS = (
    "Source/Rendering.Cratis/**", "Source/Rendering.Cratis.Scaffolding/**",
    "Source/Frontend/Runtime/**", "Source/Frontend/package*.json",
    "Verification/generated-react-browser/**",
) + RENDERER
# Both live lanes probe the same published Host image; reducer-mongo runs Host and renderer specs,
# whose Billing probe reads the Billing sample from disk.
ALWAYS_ON = {"generated-react", "standalone-generated-application"}
NO_LANE_NEEDED = ("*.md", "docs/**", "Documentation/**", ".github/ISSUE_TEMPLATE/**")
GENERATED_APPLICATION = RENDERER + HOST + ("Source/Frontend/**",)
LANE_PATHS = {
    "reducer-mongo": HOST + RENDERER + ("Samples/Billing/**",),
    "live-custom-packages": (
        "Source/Frontend/**", "Verification/live-custom-packages/**", "Verification/live-navigation/**",
        "Verification/generated-react-browser/**",
    ) + HOST_IMAGE,
    "live-blueprints": ("Verification/live-blueprints/**", "Verification/blueprint-templates/**",
                        "Verification/generated-react-browser/**") + BLUEPRINT_SOURCES + HOST_IMAGE,
    "generated-name-matrix": NAME_MATRIX,
    "generated-react-blueprint-templates": ("Verification/blueprint-templates/**",) + TEMPLATE_RENDERERS,
    # Both fixtures compile renderer output with the pinned runtime/frontend packages. Host inputs
    # include SpecRunner, BodyWorker and their linked sources; renderer inputs include its templates.
    "generated-react": GENERATED_APPLICATION + ("Verification/generated-react-browser/**",),
    "standalone-generated-application": GENERATED_APPLICATION,
}


def matching_lanes(path):
    return {lane for lane, patterns in LANE_PATHS.items()
            if any(fnmatchcase(path, pattern) for pattern in SHARED + patterns)}


def no_lane_needed(path):
    return any(fnmatchcase(path, pattern) for pattern in NO_LANE_NEEDED)


def select_lanes(changed_paths, event, labels=(), route_always_on=False):
    """Unknown inputs fail open to full coverage; narrowing is explicitly opt-in."""
    if event not in ("pull_request", "schedule", "workflow_dispatch"):
        raise ValueError(f"Unsupported CI event: {event}")
    if event != "pull_request" or "ci:full" in labels:
        return set(LANE_PATHS)
    selected = set() if route_always_on else set(ALWAYS_ON)
    for path in changed_paths:
        matching = matching_lanes(path)
        if not matching and not no_lane_needed(path):
            return set(LANE_PATHS)
        selected.update(matching)
    return selected


RENDERING_PROJECT = "Source/Rendering.Cratis/Rendering.Cratis.csproj"
HOST_PROJECT = "Source/Host/Host.csproj"
CONFORMANCE_PROJECT = "Source/Conformance.Specs/Conformance.Specs.csproj"
# Projects whose Debug output each lane consumes with `dotnet test --no-build` from the downloaded artifact.
# live-custom-packages publishes Host itself; live-blueprints runs inside that job.
LANE_BUILD_ROOTS = {
    "reducer-mongo": (RENDERING_PROJECT, HOST_PROJECT),
    "live-custom-packages": (),
    "live-blueprints": (),
    "generated-name-matrix": (CONFORMANCE_PROJECT,),
    "generated-react-blueprint-templates": (RENDERING_PROJECT,),
    "generated-react": (RENDERING_PROJECT,),
    "standalone-generated-application": (RENDERING_PROJECT,),
}
# Anything here changes what every project builds or how CI behaves: build and test everything.
BUILD_FULL_PATTERNS = (
    "*.props", "*.targets", "Source/Directory.Build.*", "global.json", "*[Nn]u[Gg]et.[Cc]onfig", "*packages.lock.json",
    "Stage.slnx", "*.sln*", ".github/workflows/**", ".github/*.py", ".github/*.json",
)
# Specs read these from disk, so no project reference describes the dependency: fail closed to full.
DISK_INPUTS = ("Samples/**", "Verification/**", "Source/Frontend/**")


class BuildPlan(NamedTuple):
    full: bool
    affected: tuple  # Solution projects the diff reaches.
    projects: tuple  # Solution projects to build: affected plus the selected lanes' roots.
    shards: tuple    # Names of the spec shards that run.


def solution_projects(root=ROOT):
    return tuple(project.attrib["Path"].replace("\\", "/") for project in ET.parse(root / "Stage.slnx").iter("Project"))


def spec_matrix():
    return runpy.run_path(str(Path(__file__).with_name("spec-shards.py")))["matrix"]()["include"]


def full_build():
    projects = solution_projects()
    return BuildPlan(True, projects, projects, tuple(shard["name"] for shard in spec_matrix()))


def affected_by(paths, projects):
    """Projects the paths reach, or None when any path is not known to be harmless."""
    inputs = {project: project_inputs(project) for project in projects}
    affected = set()
    for path in paths:
        if any(fnmatchcase(path, pattern) for pattern in BUILD_FULL_PATTERNS + DISK_INPUTS):
            return None
        reached = {project for project, patterns in inputs.items() if any(fnmatchcase(path, pattern) for pattern in patterns)}
        if not reached and not no_lane_needed(path):
            return None
        affected |= reached
    return affected


def select_build(changed_paths, event, labels=(), selected_lanes=None):
    """Pull requests build and test only what the diff reaches; everything else, and any doubt, is full."""
    if event not in ("pull_request", "schedule", "workflow_dispatch"):
        raise ValueError(f"Unsupported CI event: {event}")
    if event != "pull_request" or "ci:full" in labels:
        return full_build()
    try:
        projects = solution_projects()
        affected = affected_by(changed_paths, projects)
        if affected is None:
            return full_build()
        lanes = set(LANE_PATHS) if selected_lanes is None else selected_lanes
        built = affected | {root for lane in lanes for root in LANE_BUILD_ROOTS[lane]}
        unknown = built - set(projects)
        if unknown:
            raise ValueError(f"Not in the solution: {sorted(unknown)}")
        shards = tuple(shard["name"] for shard in spec_matrix() if affected.intersection(shard["projects"]))
        return BuildPlan(False, tuple(sorted(affected)), tuple(sorted(built)), shards)
    except Exception as error:  # Fail closed: a routing bug must never narrow the build.
        print(f"Build routing failed, building everything: {error}")
        return full_build()


def github_changes(event, payload):
    """Use both sides of renames/deletions relative to the PR merge base, not a path filter."""
    paths = []
    labels = []
    if event == "pull_request":
        base = payload["pull_request"]["base"]["ref"]
        ancestor = subprocess.check_output(["git", "merge-base", f"origin/{base}", "HEAD"], text=True).strip()
        paths = subprocess.check_output(
            ["git", "diff", "--name-only", "--no-renames", "-z", ancestor, "HEAD"], text=True).split("\0")
        # Reruns reuse the old event payload; query current labels so ci:full added
        # after the original event still takes effect. API/parse errors fail routing.
        current = json.loads(gh("pr", "view", str(payload["number"]),
                                "--repo", os.environ["GITHUB_REPOSITORY"], "--json", "labels"))
        labels = [label["name"] for label in current["labels"]]
    return [path for path in paths if path], labels


def github_plan(event, payload):
    paths, labels = github_changes(event, payload)
    lanes = select_lanes(paths, event, labels, route_always_on=os.environ.get("STAGE_ROUTE_ALWAYS_ON_LANES") == "true")
    return lanes, select_build(paths, event, labels, lanes)


def github_routes(event, payload):
    return github_plan(event, payload)[0]


def lane_selected(needs, lane):
    if needs["routing"]["result"] != "success":
        raise ValueError("Routing must succeed")
    selected = needs["routing"]["outputs"].get(lane)
    if selected not in ("true", "false"):
        raise ValueError(f"Missing/invalid routing output: {lane}")
    return selected == "true"


def require_lane(needs, lane):
    """Only an explicit false route permits a skip; failure/cancellation never passes."""
    selected = lane_selected(needs, lane)
    job = "live-custom-packages" if lane == "live-blueprints" else lane
    allowed = ("success",) if selected else ("success", "skipped")
    result = needs[job]["result"]
    if result not in allowed:
        raise ValueError(f"Lane {lane} must succeed unless explicitly unselected, not {result}")
    return selected


def require_build(needs):
    for job in ("preflight", "ci-scripts", "dotnet-build", "analyzers", "frontend", "release-pack"):
        if needs[job]["result"] != "success":
            raise ValueError(f"{job} must succeed")
    # Only an explicit routing decision that no shard is affected permits skipping the specs.
    unselected = needs["routing"]["outputs"].get("specs-selected") == "false"
    allowed = ("success", "skipped") if unselected else ("success",)
    if needs["specs"]["result"] not in allowed:
        raise ValueError("specs must succeed unless routing explicitly selected no spec shard")
    require_lane(needs, "standalone-generated-application")


def require_success(needs):
    """Fail closed on missing routing outputs, or selected jobs/steps that did not succeed."""
    for lane in LANE_PATHS.keys() - {"standalone-generated-application"}:
        selected = require_lane(needs, lane)
        if selected:
            require_live_steps(needs, lane)


def require_live_steps(needs, lane):
    live = {"live-custom-packages": ("custom-result", "navigation-result"),
            "live-blueprints": ("blueprints-result", "templates-result")}
    for step in live.get(lane, ()):
        if needs["live-custom-packages"]["outputs"].get(step) != "success":
            raise ValueError(f"Selected live probe {step} must execute and succeed")


def gh(*arguments):
    return subprocess.check_output(["gh", *arguments], text=True).strip()


REDUCER_MONGO_PROBES = ("runtime-proofs", "rendering-probes", "reconnect")

LIVE_STEPS = (
    "Run the live custom package scenarios", "Run the live navigation scenarios",
    "Run the live domain blueprint scenarios", "Run the live blueprint template scenarios",
)


def trusted_full_run(run, sha):
    """Only main's workflow is trusted; PR authors control their own workflow/job/step names."""
    expected = {"head_branch": "main", "head_sha": sha, "path": ".github/workflows/dotnet-build.yml"}
    if any(run.get(key) != value for key, value in expected.items()):
        return False
    return run.get("event") in ("push", "schedule", "workflow_dispatch")


def full_run_candidate(run, sha):
    if run.get("status") != "completed" or run.get("conclusion") != "success":
        return False
    return trusted_full_run(run, sha)


def required_full_run_jobs():
    """Only the current matrix proves full coverage; the legacy single-job name cannot replace a leg."""
    reducer_jobs = {f"reducer-mongo ({probe})" for probe in REDUCER_MONGO_PROBES}
    lanes = LANE_PATHS.keys() - {"live-blueprints", "reducer-mongo"}
    return frozenset(lanes | {"routing", "build", "routed-checks"} | reducer_jobs)


def required_jobs_succeeded(jobs, required):
    """Require every expected name and reject any non-success, including duplicate job names."""
    names = {job["name"] for job in jobs}
    if not required <= names:
        return False
    return all(job.get("conclusion") == "success" for job in jobs if job["name"] in required)


def full_run_succeeded(run, sha, jobs):
    """Require trusted main-branch metadata and every lane executing, not just a green aggregate."""
    if not full_run_candidate(run, sha):
        return False
    by_name = {job["name"]: job for job in jobs}
    if not required_jobs_succeeded(jobs, required_full_run_jobs()):
        return False
    steps = by_name["live-custom-packages"].get("steps", [])
    return set(LIVE_STEPS) <= successful_step_names(steps)


def successful_step_names(steps):
    return {step["name"] for step in steps if step["conclusion"] == "success"}


class FullCIGate:
    """Bound all GitHub requests and polling to one 55-minute release-commit check."""

    def __init__(self, repository, sha):
        self.repository, self.sha = repository, sha
        self.deadline = time.monotonic() + 55 * 60

    def request(self, *arguments):
        remaining = self.deadline - time.monotonic()
        if remaining <= 0:
            raise TimeoutError("Timed out waiting for full CI on the release commit")
        return subprocess.check_output(["gh", *arguments], text=True, timeout=min(60, remaining)).strip()

    def runs(self):
        # The API supplies run provenance; never infer trust from job or step names.
        endpoint = f"repos/{self.repository}/actions/workflows/dotnet-build.yml/runs?per_page=100"
        return json.loads(self.request("api", endpoint))["workflow_runs"]

    def succeeded(self, run):
        if not full_run_candidate(run, self.sha):
            return False
        endpoint = f"repos/{self.repository}/actions/runs/{run['id']}/attempts/{run['run_attempt']}/jobs?per_page=100"
        pages = json.loads(self.request("api", "--paginate", "--slurp", endpoint))
        return full_run_succeeded(run, self.sha, [job for page in pages for job in page["jobs"]])

    def dispatch_and_wait(self, previous):
        main = json.loads(self.request("api", f"repos/{self.repository}/git/ref/heads/main"))["object"]["sha"]
        if main != self.sha:
            raise ValueError("Release commit is no longer main and has no successful full CI run; refusing to certify another commit")
        # workflow_dispatch can trigger a run using actions:write GITHUB_TOKEN; no PAT is needed.
        self.request("workflow", "run", "dotnet-build.yml", "--repo", self.repository, "--ref", "main", "-f", "force-full=true")
        return self.wait({run["id"] for run in previous})

    def dispatched_run(self, current, known):
        candidates = [run["id"] for run in current if run["id"] not in known and run["event"] == "workflow_dispatch"
                      and trusted_full_run(run, self.sha)]
        return min(candidates, default=None)

    @staticmethod
    def completed_run(current, dispatched):
        return next((run for run in current if run["id"] == dispatched and run["status"] == "completed"), None)

    def wait(self, known):
        dispatched = None
        while time.monotonic() < self.deadline:
            current = self.runs()
            dispatched = dispatched or self.dispatched_run(current, known)
            complete = self.completed_run(current, dispatched)
            if complete:
                if not self.succeeded(complete):
                    raise ValueError(f"Full CI did not succeed for the release commit: {complete['html_url']}")
                return complete
            time.sleep(min(30, max(0, self.deadline - time.monotonic())))
        raise TimeoutError("Timed out waiting for full CI on the release commit")


def publish_gate():
    """Reuse full CI or dispatch once. Moved main without exact-SHA evidence fails closed."""
    gate = FullCIGate(os.environ["GITHUB_REPOSITORY"], os.environ["GITHUB_SHA"])
    previous = gate.runs()
    run = next((run for run in previous if gate.succeeded(run)), None)
    if run is None:
        run = gate.dispatch_and_wait(previous)
    print(f"Full CI passed for {gate.sha}: {run['html_url']}")


def nightly_body(repository, sha, run_id, green):
    """A missing baseline is explicit; never invent a last-green revision."""
    root = f"https://github.com/{repository}"
    run = f"{root}/actions/runs/{run_id}"
    if green:
        baseline = green[0]
        start = baseline["head_sha"]
        merge_range = f"Merge range since [last green nightly]({baseline['html_url']}): `{start}...{sha}`\n{root}/compare/{start}...{sha}"
    else:
        merge_range = f"No earlier green nightly exists; the merge-range baseline is unavailable. Failing revision: {root}/commit/{sha}"
    return (f"Nightly CI failed: {run}\n\n{merge_range}\n\n"
            "Conformance owner: triage within one working day; fix forward or revert. "
            "While red, PRs touching the affected lane's paths must use `ci:full`. "
            "Do not retry/quarantine without a diagnosed transient cause or relax corpus/catalog assertions.")


def gh_json(*arguments):
    """gh prints nothing (not `[]`) for an empty `label list --json` result; treat that as no items."""
    output = gh(*arguments)
    return json.loads(output) if output else []


def report_nightly():
    repository = os.environ["GITHUB_REPOSITORY"]
    dry_run = os.environ.get("NIGHTLY_DRY_RUN") == "true"
    green = json.loads(gh("api", f"repos/{repository}/actions/workflows/dotnet-build.yml/runs?event=schedule&status=success&per_page=1"))["workflow_runs"]
    body = nightly_body(repository, os.environ["GITHUB_SHA"], os.environ["GITHUB_RUN_ID"], green)
    label = "ci-nightly-red"
    issues = gh_json("issue", "list", "--repo", repository, "--state", "open", "--label", label, "--limit", "1", "--json", "number")
    if dry_run:
        print("Dry run: nothing is written.")
        action = f"comment on issue #{issues[0]['number']}" if issues else "create the label if missing and open a new issue"
        print(f"Would {action} with body:\n{body}")
        return
    if issues:
        gh("issue", "comment", str(issues[0]["number"]), "--repo", repository, "--body", body)
    else:
        # Bootstrap only a missing label; never overwrite an existing label's metadata.
        labels = gh_json("label", "list", "--repo", repository, "--search", label, "--limit", "100", "--json", "name")
        if not any(item["name"] == label for item in labels):
            gh("label", "create", label, "--repo", repository, "--color", "B60205", "--description", "Nightly CI needs conformance-owner triage")
        gh("issue", "create", "--repo", repository, "--label", label, "--title", "Nightly Stage CI is red", "--body", body)


def dry_run_table():
    scenarios = (
        ("docs-only", ["Documentation/guide.md"], "pull_request", []),
        ("Source/Frontend only", ["Source/Frontend/src/main.tsx"], "pull_request", []),
        ("Source/Host only", ["Source/Host/Program.cs"], "pull_request", []),
        ("Source/Rendering.Cratis only", ["Source/Rendering.Cratis/Renderers/TypeRenderer.cs"], "pull_request", []),
        ("Directory.Packages.props", ["Directory.Packages.props"], "pull_request", []),
        (".github/workflows only", [".github/workflows/publish.yml"], "pull_request", []),
        ("Verification/blueprint-templates only", ["Verification/blueprint-templates/model/templates.play"], "pull_request", []),
        ("nightly", [], "schedule", []),
        ("ci:full", [], "pull_request", ["ci:full"]),
        ("workflow_dispatch", [], "workflow_dispatch", []),
    )
    scenarios += (
        ("Source/Extensions leaf", ["Source/Extensions/DotNET/Foo.cs"], "pull_request", []),
        ("Source/Stage only", ["Source/Stage/Foo.cs"], "pull_request", []),
    )
    print("change set | " + " | ".join(LANE_PATHS) + " | build | projects | shards")
    for name, paths, event, labels in scenarios:
        lanes = select_lanes(paths, event, labels)
        plan = select_build(paths, event, labels, lanes)
        print(name + " | " + " | ".join("yes" if lane in lanes else "no" for lane in LANE_PATHS)
              + f" | {'full' if plan.full else 'affected'} | {len(plan.projects)} | {len(plan.shards)}")


def write_routes(selected, plan):
    outputs = {lane: str(lane in selected).lower() for lane in LANE_PATHS}
    outputs.update({
        "build-scope": "full" if plan.full else "affected",
        "affected-projects": json.dumps(plan.affected),
        "build-projects": json.dumps(plan.projects),
        "specs-selected": str(bool(plan.shards)).lower(),
        "spec-shards": json.dumps(plan.shards),
    })
    with open(os.environ["GITHUB_OUTPUT"], "a") as output:
        for key, value in outputs.items():
            output.write(f"{key}={value}\n")
    print("Selected lanes: " + ", ".join(sorted(selected)))
    print(f"Build scope: {outputs['build-scope']}; projects: {len(plan.projects)}; spec shards: {', '.join(plan.shards) or 'none'}")


def write_solution_filter(projects):
    """A .slnf listing the projects to build; MSBuild adds their ProjectReference dependencies."""
    chosen = json.loads(projects)
    unknown = set(chosen) - set(solution_projects())
    if not chosen or unknown:
        raise ValueError(f"Solution filter needs solution projects, not {sorted(unknown) or 'none'}")
    print(json.dumps({"solution": {"path": str(ROOT / "Stage.slnx"), "projects": sorted(chosen)}}, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("route", "check", "check-build", "publish-gate", "table", "report-nightly", "write-slnf"))
    parser.add_argument("argument", nargs="?")
    args = parser.parse_args()
    if args.command == "write-slnf":
        write_solution_filter(args.argument)
    elif args.command == "route":
        selected, plan = github_plan(os.environ["GITHUB_EVENT_NAME"], json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text()))
        write_routes(selected, plan)
    elif args.command == "check":
        needs = json.loads(os.environ["NEEDS_JSON"])
        require_success(needs)
        count = sum(needs["routing"]["outputs"].get(lane) == "true" for lane in LANE_PATHS)
        print(f"Required {count} selected lanes: all succeeded")
    elif args.command == "check-build":
        require_build(json.loads(os.environ["NEEDS_JSON"]))
    elif args.command == "publish-gate":
        publish_gate()
    elif args.command == "table":
        dry_run_table()
    else:
        report_nightly()


if __name__ == "__main__":
    main()
