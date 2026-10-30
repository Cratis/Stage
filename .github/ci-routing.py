#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Select optional CI lanes, require their success, and report failed nightlies.

For a full PR run: add ci:full, then push or use "Re-run all jobs" on the dotnet-build workflow
("Re-run failed jobs" does not rerun routing, so it ignores the label).
"""

import argparse
from fnmatch import fnmatchcase
import json
import os
from pathlib import Path
import posixpath
import re
import subprocess

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
    "Verification/verify-packed-third-party-planner.py",
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
}


def select_lanes(changed_paths, event, labels=()):
    """Pure routing policy; shared inputs select every lane, including blueprint probes."""
    if event not in ("pull_request", "schedule", "workflow_dispatch"):
        raise ValueError(f"Unsupported CI event: {event}")
    if event != "pull_request" or "ci:full" in labels:
        return set(LANE_PATHS)
    return {lane for lane, patterns in LANE_PATHS.items()
            if any(fnmatchcase(path, pattern) for path in changed_paths for pattern in SHARED + patterns)}


def github_routes(event, payload):
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
    return select_lanes(paths, event, labels)


def require_success(needs):
    """Fail closed on missing routing outputs, or selected jobs/steps that did not succeed."""
    routing = needs["routing"]
    if routing["result"] != "success":
        raise ValueError("Routing must succeed")
    outputs = routing["outputs"]
    for lane in LANE_PATHS:
        selected = outputs.get(lane)
        if selected not in ("true", "false"):
            raise ValueError(f"Missing/invalid routing output: {lane}")
        if selected == "false":
            continue
        job = "live-custom-packages" if lane == "live-blueprints" else lane
        if needs[job]["result"] != "success":
            raise ValueError(f"Selected lane {lane} must succeed, not {needs[job]['result']}")
        if lane in ("live-custom-packages", "live-blueprints"):
            steps = ("custom-result", "navigation-result") if lane == "live-custom-packages" else ("blueprints-result", "templates-result")
            for step in steps:
                if needs[job]["outputs"].get(step) != "success":
                    raise ValueError(f"Selected live probe {step} must execute and succeed")
    if needs["generated-react"]["result"] != "success":
        raise ValueError("The always-on generated-react smoke must succeed")


def gh(*arguments):
    return subprocess.check_output(["gh", *arguments], text=True).strip()


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


def report_nightly():
    repository = os.environ["GITHUB_REPOSITORY"]
    green = json.loads(gh("api", f"repos/{repository}/actions/workflows/dotnet-build.yml/runs?event=schedule&status=success&per_page=1"))["workflow_runs"]
    body = nightly_body(repository, os.environ["GITHUB_SHA"], os.environ["GITHUB_RUN_ID"], green)
    label = "ci-nightly-red"
    issues = json.loads(gh("issue", "list", "--repo", repository, "--state", "open", "--label", label, "--limit", "1", "--json", "number"))
    if issues:
        gh("issue", "comment", str(issues[0]["number"]), "--repo", repository, "--body", body)
    else:
        # Bootstrap only a missing label; never overwrite an existing label's metadata.
        labels = json.loads(gh("label", "list", "--repo", repository, "--search", label, "--limit", "100", "--json", "name"))
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
    print("change set | " + " | ".join(LANE_PATHS))
    for name, paths, event, labels in scenarios:
        lanes = select_lanes(paths, event, labels)
        print(name + " | " + " | ".join("yes" if lane in lanes else "no" for lane in LANE_PATHS))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("route", "check", "table", "report-nightly"))
    args = parser.parse_args()
    if args.command == "route":
        selected = github_routes(os.environ["GITHUB_EVENT_NAME"], json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text()))
        with open(os.environ["GITHUB_OUTPUT"], "a") as output:
            for lane in LANE_PATHS:
                output.write(f"{lane}={str(lane in selected).lower()}\n")
        print("Selected lanes: " + ", ".join(sorted(selected)))
    elif args.command == "check":
        needs = json.loads(os.environ["NEEDS_JSON"])
        require_success(needs)
        count = sum(value == "true" for value in needs["routing"]["outputs"].values())
        print(f"Required {count} selected lanes and the always-on generated-react smoke: all succeeded")
    elif args.command == "table":
        dry_run_table()
    else:
        report_nightly()


if __name__ == "__main__":
    main()
