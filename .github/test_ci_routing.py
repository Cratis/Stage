#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Exercise path policy, required-check safety, and nightly issue reporting without GitHub effects."""

import contextlib
import copy
import io
import json
import os
from pathlib import Path
import re
import runpy
import unittest
from unittest.mock import patch

CI = runpy.run_path(str(Path(__file__).with_name("ci-routing.py")))
ALL = set(CI["LANE_PATHS"])
WORKFLOW = Path(__file__).with_name("workflows").joinpath("dotnet-build.yml").read_text()
SECTIONS = re.split(r"^  ([\w-]+):\n", WORKFLOW.split("jobs:\n", 1)[1], flags=re.MULTILINE)
JOBS = dict(zip(SECTIONS[1::2], SECTIONS[2::2]))
DRAFT_GATE = "github.event_name != 'pull_request' || github.event.pull_request.draft == false"


class RoutingTests(unittest.TestCase):
    def select(self, *paths, event="pull_request", labels=()):
        return CI["select_lanes"](paths, event, labels)

    def test_docs_select_no_optional_lanes(self):
        self.assertEqual(set(), self.select("Documentation/guide.md", "README.md"))

    def test_manual_nightly_label_and_dependency_pins_select_every_lane(self):
        for event in ("schedule", "workflow_dispatch"):
            self.assertEqual(ALL, self.select(event=event))
        self.assertEqual(ALL, self.select(labels=["ci:full"]))
        self.assertEqual(ALL, self.select("Directory.Packages.props"))
        self.assertEqual(set(), self.select(labels=["patch"]))
        with self.assertRaises(ValueError):
            self.select(event="push")

    def test_every_shared_path_selects_every_lane(self):
        for path in ("Directory.Packages.props", "nested/custom.props", "nested/custom.targets",
                     "Source/Directory.Build.targets", "Source/Directory.Build.rsp", "global.json", "NuGet.config", "Source/Host/packages.lock.json",
                     "Source/Contracts/Scene/Screen.cs", "Source/Host/Dockerfile",
                     "Verification/verify-runtime-compatibility.py", "Verification/synchronize-rendered-pins.py",
                     "Verification/verify-packed-third-party-planner.py",
                     ".github/workflows/publish.yml", ".github/ci-routing.py", ".github/test_spec_shards.py"):
            with self.subTest(path=path):
                self.assertEqual(ALL, self.select(path))

    def test_host_frontend_and_renderer_paths_are_routed_independently(self):
        self.assertEqual({"reducer-mongo", "live-custom-packages", "live-blueprints"}, self.select("Source/Host/Program.cs"))
        # The Host image ships the whole frontend, and both live lanes probe that image.
        self.assertEqual({"live-custom-packages", "live-blueprints"}, self.select("Source/Frontend/src/main.tsx"))
        for folder in ("Semantics", "Renderers", "Specifications", "Types", "Emission", "Validation", "Authorization", "NewDependency"):
            with self.subTest(folder=folder):
                self.assertEqual({"reducer-mongo", "generated-name-matrix", "generated-react-blueprint-templates"},
                                 self.select(f"Source/Rendering.Cratis/{folder}/Implementation.cs"))
        self.assertEqual({"reducer-mongo", "generated-name-matrix", "generated-react-blueprint-templates"},
                         self.select("Source/Rendering.Cratis.Scaffolding/Scaffold.cs"))
        # Specifications (name matrix) references Stage; the Host image compiles it.
        self.assertEqual({"reducer-mongo", "live-custom-packages", "live-blueprints", "generated-name-matrix"},
                         self.select("Source/Stage/Runtime/Query.cs"))

    def test_live_probe_inputs_and_blueprint_pins(self):
        for folder in ("live-custom-packages", "live-navigation"):
            self.assertEqual({"live-custom-packages"}, self.select(f"Verification/{folder}/run.sh"))
        self.assertEqual({"live-blueprints"}, self.select("Verification/live-blueprints/models/CRM/CRM.play"))
        self.assertEqual({"live-blueprints", "generated-react-blueprint-templates"},
                         self.select("Verification/blueprint-templates/model/templates.play"))
        for path in ("Source/Frontend/package.json", "Source/Frontend/package-lock.json",
                     "Source/Frontend/Runtime/package.json", "Source/Frontend/Runtime/src/blueprint.ts",
                     "Source/Rendering.Cratis/Shipped/ShippedPackages.json",
                     "Source/Rendering.Cratis/Scaffolding/CratisFrontendPackageSet.cs"):
            with self.subTest(path=path):
                self.assertIn("live-blueprints", self.select(path))
                self.assertIn("generated-react-blueprint-templates", self.select(path))

    def test_every_enumerated_name_and_template_pattern_is_selected(self):
        # Exercise every explicit pattern, including hypothetical new siblings in audited folders.
        for lane, patterns in (("generated-name-matrix", CI["NAME_MATRIX"]),
                               ("generated-react-blueprint-templates", CI["TEMPLATE_RENDERERS"])):
            for pattern in patterns:
                path = pattern.replace("**", "new/subtree").replace("*", "New")
                with self.subTest(lane=lane, path=path):
                    self.assertIn(lane, self.select(path))
        self.assertEqual({"generated-name-matrix"},
                         self.select("Source/Conformance.Specs/for_ThreeWayConformance/when_compiling_generated_name_matrix.cs"))

    def test_dependency_globs_still_match_real_sources(self):
        root = Path(__file__).resolve().parent.parent
        for pattern in CI["NAME_MATRIX"] + CI["TEMPLATE_RENDERERS"] + CI["BLUEPRINT_SOURCES"]:
            with self.subTest(pattern=pattern):
                # Before Python 3.13 a trailing "**" matches only directories, so look one level into them.
                files = pattern + "/*" if pattern.endswith("**") else pattern
                self.assertTrue(any(path.is_file() for path in root.glob(files)), pattern)
        reducer = "Source/Rendering.Cratis/for_CratisArtifactRenderPlanner/when_rendering_a_pure_reducer.cs"
        self.assertIn("generated-name-matrix", self.select(reducer))

    def test_cross_project_build_inputs_select_every_consuming_lane(self):
        live = {"reducer-mongo", "live-custom-packages", "live-blueprints"}
        for path, expected in (
                ("Source/Specifications/Commands/SemanticValueShapeAdmission.cs", live | {"generated-name-matrix"}),
                ("Source/SpecRunner/Program.cs", live),
                ("Source/Rendering.Cratis/Naming/GeneratedTypeNames.cs", ALL),
                ("Source/Rendering.Cratis/Semantics/SemanticVersionFeatures.cs", ALL),
                ("Source/Stage/Runtime/PortableTenantValues.cs", ALL),
                ("Samples/Billing/Ledger/Payments/Payments.play", {"reducer-mongo"}),
                ("Source/Conformance.Specs/for_UiProfileRootVector/uiprofile-root.scene.json",
                 {"live-custom-packages", "live-blueprints"})):
            with self.subTest(path=path):
                self.assertEqual(expected, self.select(path))

    def test_every_linked_or_published_project_input_routes_to_its_consumers(self):
        # Reads the real project files, so a new cross-folder link can never bypass routing.
        root = Path(__file__).resolve().parent.parent
        link = re.compile(r'<(?:ProjectReference|Compile|EmbeddedResource|Content|None)\s+Include="(\.\./[^"]+)"')
        consumers = {"Source/Host/Host.csproj": {"reducer-mongo", "live-custom-packages", "live-blueprints"},
                     "Source/Stage/Stage.csproj": {"reducer-mongo", "live-custom-packages", "live-blueprints", "generated-name-matrix"},
                     "Source/Specifications/Specifications.csproj": {"generated-name-matrix", "reducer-mongo", "live-custom-packages"},
                     "Source/SpecRunner/SpecRunner.csproj": {"reducer-mongo", "live-custom-packages", "live-blueprints"},
                     "Source/Rendering.Cratis/Rendering.Cratis.csproj": {"reducer-mongo", "generated-name-matrix", "generated-react-blueprint-templates"}}
        for project, lanes in consumers.items():
            folder = Path(project).parent
            for match in link.findall((root / project).read_text()):
                for include in match.split(";"):
                    target = Path(os.path.normpath(folder / include))
                    candidates = [target.parent / "Linked.cs"] if target.suffix == ".csproj" else \
                        [path.relative_to(root) for path in root.glob(target.as_posix())]
                    self.assertTrue(candidates, include)
                    for candidate in candidates:
                        with self.subTest(project=project, path=candidate.as_posix()):
                            self.assertLessEqual(lanes, self.select(candidate.as_posix()))
        self.assertLessEqual({"reducer-mongo", "live-custom-packages", "live-blueprints"},
                             self.select("Source/SpecRunner/New.cs"))

    def test_project_inputs_follow_references_links_and_nested_publishes(self):
        import tempfile
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            files = {
                "Source/App/App.csproj": '<ProjectReference Include="../Lib/Lib.csproj" />'
                                         '<Exec Command="dotnet publish &quot;$(MSBuildThisFileDirectory)../Tool/Tool.csproj&quot;" />',
                "Source/Lib/Lib.csproj": '<Compile Include="../Other/Shared.cs;../Other/More.cs" />'
                                         '<EmbeddedResource Include="../Web/src/*.ts" /><Compile Include="Local.cs" />',
                "Source/Tool/Tool.csproj": '<ProjectReference Include="../App/App.csproj" />',
            }
            for name, text in files.items():
                (root / name).parent.mkdir(parents=True, exist_ok=True)
                (root / name).write_text(text)
            self.assertEqual(
                ("Source/App/**", "Source/Lib/**", "Source/Other/More.cs", "Source/Other/Shared.cs",
                 "Source/Tool/**", "Source/Web/src/*.ts"),
                CI["project_inputs"]("Source/App/App.csproj", root=root))
            self.assertNotIn("Source/App/**", CI["project_inputs"]("Source/Lib/Lib.csproj", root=root))

    def test_image_inputs_cover_copied_sources_but_not_build_stages_or_output(self):
        import tempfile
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ("Source/Web/index.ts", "Source/Host/app.json", "Source/Data/a.json"):
                (root / name).parent.mkdir(parents=True, exist_ok=True)
                (root / name).write_text("{}")
            (root / "Dockerfile").write_text(
                "COPY Source/Web/ ./\nCOPY --chown=1 Source/Host/app.json Source/Data /x/\n"
                "COPY Source/Host/out ./\nCOPY --from=frontend /dist ./wwwroot\nRUN COPY nothing\n")
            self.assertEqual(("Source/Data/**", "Source/Host/app.json", "Source/Web/**"),
                             CI["image_inputs"]("Dockerfile", root=root))

    def test_multiple_changes_union_lanes_without_prefix_false_positives(self):
        self.assertEqual({"live-custom-packages", "live-blueprints", "generated-name-matrix", "reducer-mongo"},
                         self.select("Source/Frontend/src/main.tsx", "Source/Specifications/Executor.cs"))
        self.assertEqual(set(), self.select("Source/Hostile/Program.cs", "docs/global.json.example"))

    def test_github_diff_uses_merge_base_and_both_rename_paths(self):
        payload = {"number": 562, "pull_request": {"base": {"ref": "main"}, "labels": []}}
        responses = ["abc\n", "Source/Host/Old.cs\0Documentation/New.md\0", '{"labels": []}']
        with patch.dict(os.environ, GITHUB_REPOSITORY="Cratis/Stage"), \
                patch("subprocess.check_output", side_effect=responses) as run:
            self.assertEqual({"reducer-mongo", "live-custom-packages", "live-blueprints"}, CI["github_routes"]("pull_request", payload))
        self.assertEqual(["git", "merge-base", "origin/main", "HEAD"], run.call_args_list[0].args[0])
        self.assertEqual(["git", "diff", "--name-only", "--no-renames", "-z", "abc", "HEAD"], run.call_args_list[1].args[0])
        self.assertEqual(["gh", "pr", "view", "562", "--repo", "Cratis/Stage", "--json", "labels"], run.call_args_list[2].args[0])
        for event in ("schedule", "workflow_dispatch"):
            with patch("subprocess.check_output") as run:
                self.assertEqual(ALL, CI["github_routes"](event, {}))
                run.assert_not_called()

    def test_reruns_use_current_api_labels_instead_of_stale_event_labels(self):
        for old, current, expected in (([], [{"name": "patch"}, {"name": "ci:full"}], ALL),
                                       ([{"name": "ci:full"}], [{"name": "patch"}], set())):
            payload = {"number": 562, "pull_request": {"base": {"ref": "main"}, "labels": old}}
            responses = ["abc\n", "Documentation/guide.md\0", json.dumps({"labels": current})]
            with self.subTest(current=current), patch.dict(os.environ, GITHUB_REPOSITORY="Cratis/Stage"), \
                    patch("subprocess.check_output", side_effect=responses):
                self.assertEqual(expected, CI["github_routes"]("pull_request", payload))

    def test_label_lookup_failure_or_malformed_response_fails_routing(self):
        payload = {"number": 562, "pull_request": {"base": {"ref": "main"}, "labels": []}}
        for response, error in ((RuntimeError("GitHub unavailable"), RuntimeError),
                                ("not JSON", ValueError), ("{}", KeyError),
                                ('{"labels": [{}]}', KeyError)):
            with self.subTest(response=response), patch.dict(os.environ, GITHUB_REPOSITORY="Cratis/Stage"), \
                    patch("subprocess.check_output", side_effect=["abc\n", "Documentation/guide.md\0", response]), \
                    self.assertRaises(error):
                CI["github_routes"]("pull_request", payload)

    def test_workflow_skips_docs_without_label_triggers_and_separates_event_concurrency(self):
        triggers = WORKFLOW.split("jobs:\n", 1)[0]
        self.assertIn("paths:", triggers)
        # GitHub does not allow paths-ignore alongside the existing source allowlist.
        self.assertNotIn("paths-ignore:", triggers)
        for path in ('"Source/**"', '".github/ci-routing.py"', '".github/test_ci_routing.py"', '".github/spec-shards.py"'):
            self.assertIn(path, triggers)
        self.assertNotIn('"Documentation/**"', triggers)
        self.assertEqual(1, triggers.count("- cron:"))
        self.assertIn("types: [opened, synchronize, reopened, ready_for_review]", triggers)
        self.assertNotIn("labeled", triggers)
        self.assertIn("group: ${{ github.workflow }}-${{ github.event_name }}-${{ github.ref }}", triggers)
        self.assertIn("cancel-in-progress: true", triggers)
        usage = 'add ci:full, then push or use "Re-run all jobs" on the dotnet-build workflow'
        self.assertIn(usage, triggers)
        self.assertIn(usage, CI["__doc__"])
        self.assertIn("fetch-depth: 0", JOBS["routing"])
        self.assertIn("pull-requests: read", JOBS["routing"])
        self.assertIn("GH_TOKEN: ${{ github.token }}", JOBS["routing"])
        self.assertIn("needs: routing", JOBS["preflight"])
        for job in ("routing", "preflight", "dotnet-build", "specs"):
            self.assertIn(f"    if: ${{{{ {DRAFT_GATE} }}}}\n", JOBS[job], job)
        for lane in ALL - {"live-custom-packages", "live-blueprints"}:
            self.assertIn(f"\n      && needs.routing.outputs.{lane} == 'true'\n", JOBS[lane])
        live = JOBS["live-custom-packages"]
        self.assertIn("\n      && (needs.routing.outputs.live-custom-packages == 'true' || needs.routing.outputs.live-blueprints == 'true')\n", live)
        self.assertEqual(1, live.count("run: docker build"))
        for step, lane in (("custom", "live-custom-packages"), ("navigation", "live-custom-packages"),
                           ("blueprints", "live-blueprints"), ("templates", "live-blueprints")):
            self.assertIn(f"id: {step}\n        if: needs.routing.outputs.{lane} == 'true'", live)
            self.assertIn(f"${{{{ steps.{step}.outcome }}}}", live)
        for job in ("build", "routed-checks"):
            self.assertIn(f"    if: always() && ({DRAFT_GATE})\n", JOBS[job])
        self.assertIn("NEEDS_JSON: ${{ toJson(needs) }}", JOBS["routed-checks"])
        for job in ("routing", "reducer-mongo", "generated-name-matrix", "live-custom-packages",
                    "generated-react", "generated-react-blueprint-templates"):
            self.assertIn(job, JOBS["routed-checks"].splitlines()[0])
        self.assertIn("github.event_name == 'schedule'", JOBS["nightly-red"])
        self.assertIn("needs.build.result != 'success' || needs.routed-checks.result != 'success'", JOBS["nightly-red"])
        self.assertEqual(1, WORKFLOW.count("issues: write"))
        self.assertIn("issues: write", JOBS["nightly-red"])

    def test_every_job_gates_draft_pull_requests_without_skipping_other_events(self):
        self.assertTrue(JOBS)
        for job, definition in JOBS.items():
            with self.subTest(job=job):
                self.assertIn(DRAFT_GATE, definition)

    def test_pull_requests_start_heavy_lanes_only_after_build_and_specs(self):
        gate = f"""    needs: [preflight, routing, dotnet-build, specs]
    # Pull requests start heavy lanes only after build and specs pass; nightly/manual runs keep full coverage.
    if: >-
      !cancelled() && ({DRAFT_GATE}) && needs.preflight.result == 'success' && needs.routing.result == 'success'
      && (github.event_name != 'pull_request' || (needs.dotnet-build.result == 'success' && needs.specs.result == 'success'))"""
        standalone_gate = f"""    needs: [preflight, routing, dotnet-build]
    # Unlike the other heavy lanes, this one starts as soon as the build passes, next to the spec shards: it is
    # part of the required `build` aggregate, so waiting for the shards first put both on the critical path.
    if: >-
      !cancelled() && ({DRAFT_GATE}) && needs.preflight.result == 'success' && needs.routing.result == 'success'
      && (github.event_name != 'pull_request' || needs.dotnet-build.result == 'success')"""
        heavy = (ALL - {"live-blueprints"}) | {"generated-react"}
        self.assertEqual(5, len(heavy))
        for job in heavy:
            self.assertTrue(JOBS[job].startswith(gate), job)
        self.assertTrue(JOBS["generated-react"].startswith(gate + "\n    runs-on:"), "generated-react")
        self.assertTrue(
            JOBS["standalone-generated-application"].startswith(standalone_gate + "\n    runs-on:"),
            "standalone-generated-application")
        self.assertIn("standalone-generated-application", JOBS["build"].splitlines()[0])
        self.assertIn("STANDALONE_RESULT\" = success", JOBS["build"])

    def test_dry_run_table_covers_requested_scenarios(self):
        with contextlib.redirect_stdout(io.StringIO()) as output:
            CI["dry_run_table"]()
        self.assertEqual(11, len(output.getvalue().splitlines()))
        self.assertIn("ci:full | yes | yes | yes | yes | yes", output.getvalue())
        self.assertIn("Source/Rendering.Cratis only | yes | no | no | yes | yes", output.getvalue())


class RequiredChecksTests(unittest.TestCase):
    def needs(self, selected):
        needs = {job: {"result": "success", "outputs": {}} for job in ALL | {"routing", "generated-react"}}
        needs["routing"]["outputs"] = {lane: str(lane in selected).lower() for lane in ALL}
        needs["live-custom-packages"]["outputs"] = dict.fromkeys(
            ("custom-result", "navigation-result", "blueprints-result", "templates-result"), "success")
        return needs

    def test_all_selected_lanes_must_succeed_not_skip(self):
        needs = self.needs(ALL)
        CI["require_success"](needs)
        for job in ALL - {"live-blueprints"} | {"routing", "generated-react"}:
            for result in ("failure", "skipped", "cancelled", ""):
                changed = copy.deepcopy(needs)
                changed[job]["result"] = result
                with self.subTest(job=job, result=result), self.assertRaises(ValueError):
                    CI["require_success"](changed)

    def test_unselected_lanes_may_skip_but_routing_and_smoke_are_required(self):
        needs = self.needs(set())
        for lane in ALL - {"live-blueprints"}:
            needs[lane]["result"] = "skipped"
        CI["require_success"](needs)
        for lane in ALL:
            changed = copy.deepcopy(needs)
            changed["routing"]["outputs"][lane] = "true"
            with self.subTest(lane=lane), self.assertRaises(ValueError):
                CI["require_success"](changed)

    def test_missing_or_invalid_output_fails_closed_even_with_successful_jobs(self):
        for lane in ALL:
            for value in (None, "", "TRUE", "skipped"):
                needs = self.needs(ALL)
                needs["routing"]["outputs"][lane] = value
                with self.subTest(lane=lane, value=value), self.assertRaises(ValueError):
                    CI["require_success"](needs)

    def test_split_live_probes_must_actually_execute(self):
        for selected, required, optional in (
            ({"live-custom-packages"}, ("custom-result", "navigation-result"), ("blueprints-result", "templates-result")),
            ({"live-blueprints"}, ("blueprints-result", "templates-result"), ("custom-result", "navigation-result")),
        ):
            needs = self.needs(selected)
            for step in optional:
                needs["live-custom-packages"]["outputs"][step] = "skipped"
            CI["require_success"](needs)
            for step in required:
                for result in ("failure", "skipped", "cancelled", ""):
                    changed = copy.deepcopy(needs)
                    changed["live-custom-packages"]["outputs"][step] = result
                    with self.subTest(step=step, result=result), self.assertRaises(ValueError):
                        CI["require_success"](changed)


class NightlyTests(unittest.TestCase):
    def test_body_links_last_green_range_and_does_not_invent_missing_baseline(self):
        green = [{"head_sha": "green", "html_url": "https://github.com/Cratis/Stage/actions/runs/1"}]
        body = CI["nightly_body"]("Cratis/Stage", "red", "2", green)
        self.assertIn("actions/runs/2", body)
        self.assertIn("green...red", body)
        self.assertIn("https://github.com/Cratis/Stage/compare/green...red", body)
        self.assertIn("actions/runs/1", body)
        self.assertIn("ci:full", body)
        missing = CI["nightly_body"]("Cratis/Stage", "red", "2", [])
        self.assertIn("baseline is unavailable", missing)
        self.assertNotIn("/compare/", missing)

    def report(self, responses):
        with patch.dict(os.environ, GITHUB_REPOSITORY="Cratis/Stage", GITHUB_SHA="red", GITHUB_RUN_ID="2"), \
                patch.dict(CI["report_nightly"].__globals__, {"gh": unittest.mock.Mock(side_effect=responses)}) as scope:
            CI["report_nightly"]()
            return scope["gh"].call_args_list

    def test_comments_on_existing_open_issue(self):
        calls = self.report([json.dumps({"workflow_runs": []}), '[{"number": 42}]', ""])
        self.assertIn("event=schedule&status=success&per_page=1", calls[0].args[1])
        self.assertEqual(("issue", "comment", "42"), calls[2].args[:3])
        self.assertEqual(3, len(calls))

    def test_creates_label_only_when_missing_then_opens_issue(self):
        for labels, expected in (([], 5), ([{"name": "ci-nightly-red"}], 4)):
            responses = [json.dumps({"workflow_runs": []}), "[]", json.dumps(labels), "", ""]
            calls = self.report(responses)
            self.assertEqual(expected, len(calls))
            self.assertEqual(("issue", "create"), calls[-1].args[:2])
            if not labels:
                self.assertEqual(("label", "create", "ci-nightly-red"), calls[-2].args[:3])

    def test_api_failure_propagates_without_retry_or_issue_mutation(self):
        with self.assertRaises(RuntimeError):
            self.report([RuntimeError("GitHub unavailable")])


if __name__ == "__main__":
    unittest.main()
