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
import textwrap
import unittest
import xml.etree.ElementTree as ET
from unittest.mock import mock_open, patch

CI = runpy.run_path(str(Path(__file__).with_name("ci-routing.py")))
ALL = set(CI["LANE_PATHS"])
ALWAYS_ON = CI["ALWAYS_ON"]
CI_SHARDS = runpy.run_path(str(Path(__file__).with_name("spec-shards.py")))
ROUTED = ALL - {"standalone-generated-application"}
WORKFLOW = Path(__file__).with_name("workflows").joinpath("dotnet-build.yml").read_text()
SECTIONS = re.split(r"^  ([\w-]+):\n", WORKFLOW.split("jobs:\n", 1)[1], flags=re.MULTILINE)
JOBS = dict(zip(SECTIONS[1::2], SECTIONS[2::2]))
DRAFT_GATE = "github.event_name != 'pull_request' || github.event.pull_request.draft == false"
SKIP_GATE = "needs.routing.outputs.skip != 'true'"


class RoutingTests(unittest.TestCase):
    def setUp(self):
        self.environment = patch.dict(os.environ, STAGE_ROUTE_ALWAYS_ON_LANES="true", GITHUB_REPOSITORY="Cratis/Stage")
        self.environment.start()
        self.addCleanup(self.environment.stop)

    def select(self, *paths, event="pull_request", labels=()):
        return CI["select_lanes"](paths, event, labels, route_always_on=True)

    def test_narrowing_is_explicitly_opt_in_and_other_lanes_are_unchanged(self):
        for path in ("README.md", "Verification/live-navigation/run.sh"):
            narrowed = self.select(path)
            self.assertEqual(narrowed | ALWAYS_ON, CI["select_lanes"]([path], "pull_request"))
        payload = {"number": 632, "pull_request": {"base": {"ref": "main"}}}
        for value in ("", "false", "TRUE", "true"):
            with self.subTest(value=value), patch.dict(os.environ, STAGE_ROUTE_ALWAYS_ON_LANES=value), \
                    patch("subprocess.check_output", side_effect=["base", "README.md\0", '{"labels": []}']):
                expected = set() if value == "true" else ALWAYS_ON
                self.assertEqual(expected, CI["github_routes"]("pull_request", payload))

    def test_unmatched_paths_force_full_even_alongside_known_or_allowlisted_inputs(self):
        for path in ("Source/Hostile/Program.cs", "new-tool/config.json", "Samples/New/app.play", "README.txt"):
            for other in ([], ["README.md"], ["Verification/live-navigation/run.sh"]):
                with self.subTest(path=path, other=other):
                    self.assertEqual(ALL, self.select(path, *other))
        for path in ("README.md", "Source/Unrouted/notes.md", "docs/config.json", "Documentation/guide.html",
                     ".github/ISSUE_TEMPLATE/bug.yml"):
            self.assertEqual(set(), self.select(path), path)

    def test_both_new_lanes_cover_renderer_host_templates_frontend_and_pins(self):
        for pattern in CI["GENERATED_APPLICATION"]:
            path = pattern.replace("**", "new/subtree").replace("*", "New")
            self.assertLessEqual(ALWAYS_ON, self.select(path), path)
        self.assertIn("generated-react", self.select("Verification/generated-react-browser/run.sh"))
        self.assertNotIn("standalone-generated-application", self.select("Verification/generated-react-browser/run.sh"))

    def test_outputs_include_each_lane_without_a_tested_merge_sha_claim(self):
        for selected in (ALL, set(), {"generated-react"}):
            output = mock_open()
            plan = CI["select_build"](["README.md"], "pull_request", (), selected)
            with patch.dict(os.environ, GITHUB_OUTPUT="unused"), patch("builtins.open", output), \
                    patch("subprocess.check_output") as git, \
                    contextlib.redirect_stdout(io.StringIO()):
                CI["write_routes"](selected, plan)
            written = "".join(call.args[0] for call in output().write.call_args_list)
            for lane in ALL:
                self.assertIn(f"{lane}={str(lane in selected).lower()}\n", written)
            self.assertNotIn("full-sha=", written)
            git.assert_not_called()

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
                     ".github/workflows/publish.yml", ".github/ci-routing.py", ".github/test_spec_shards.py"):
            with self.subTest(path=path):
                self.assertEqual(ALL, self.select(path))

    def test_host_frontend_and_renderer_paths_are_routed_independently(self):
        self.assertEqual({"reducer-mongo", "live-custom-packages", "live-blueprints"} | ALWAYS_ON, self.select("Source/Host/Program.cs"))
        # The Host image ships the whole frontend, and both live lanes probe that image.
        self.assertEqual({"live-custom-packages", "live-blueprints"} | ALWAYS_ON, self.select("Source/Frontend/src/main.tsx"))
        for folder in ("Semantics", "Renderers", "Specifications", "Types", "Emission", "Validation", "Authorization", "NewDependency"):
            with self.subTest(folder=folder):
                self.assertEqual(ALL, self.select(f"Source/Rendering.Cratis/{folder}/Implementation.cs"))
        self.assertEqual({"reducer-mongo", "generated-name-matrix", "generated-react-blueprint-templates"} | ALWAYS_ON,
                         self.select("Source/Rendering.Cratis.Scaffolding/Scaffold.cs"))
        # Specifications (name matrix) references Stage; the Host image compiles it.
        self.assertEqual({"reducer-mongo", "live-custom-packages", "live-blueprints", "generated-name-matrix"} | ALWAYS_ON,
                         self.select("Source/Stage/Runtime/Query.cs"))

    def test_host_inputs_follow_the_published_body_worker_to_the_renderer(self):
        # Host publishes BodyWorker, whose renderer reference changes the image's runtime behavior.
        inputs = CI["project_inputs"]("Source/Host/Host.csproj")
        self.assertIn("Source/BodyWorker/**", inputs)
        self.assertIn("Source/Rendering.Cratis/**", inputs)
        self.assertLessEqual({"reducer-mongo", "live-custom-packages", "live-blueprints"},
                             self.select("Source/BodyWorker/Program.cs"))

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
                ("Source/Specifications/Commands/SemanticValueShapeAdmission.cs", live | ALWAYS_ON | {"generated-name-matrix"}),
                ("Source/SpecRunner/Program.cs", live | ALWAYS_ON),
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
        self.assertEqual({"live-custom-packages", "live-blueprints", "generated-name-matrix", "reducer-mongo"} | ALWAYS_ON,
                         self.select("Source/Frontend/src/main.tsx", "Source/Specifications/Executor.cs"))
        self.assertEqual(ALL, self.select("Source/Hostile/Program.cs", "docs/global.json.example"))

    def test_github_diff_uses_merge_base_and_both_rename_paths(self):
        payload = {"number": 562, "pull_request": {"base": {"ref": "main"}, "labels": []}}
        responses = ["abc\n", "Source/Host/Old.cs\0Documentation/New.md\0", '{"labels": []}']
        with patch.dict(os.environ, GITHUB_REPOSITORY="Cratis/Stage"), \
                patch("subprocess.check_output", side_effect=responses) as run:
            self.assertEqual({"reducer-mongo", "live-custom-packages", "live-blueprints"} | ALWAYS_ON, CI["github_routes"]("pull_request", payload))
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
        release_dispatch = "vars.STAGE_ROUTE_ALWAYS_ON_LANES == 'true' && github.event_name == 'workflow_dispatch' && inputs.force-full"
        self.assertIn(f"cancel-in-progress: ${{{{ !({release_dispatch}) }}}}", triggers)
        self.assertIn(release_dispatch + " && format('-release-{0}', github.run_id) || ''", triggers)
        usage = 'add ci:full, then push or use "Re-run all jobs" on the dotnet-build workflow'
        self.assertIn(usage, triggers)
        self.assertIn(usage, CI["__doc__"])
        self.assertIn("fetch-depth: 0", JOBS["routing"])
        self.assertIn("pull-requests: read", JOBS["routing"])
        self.assertIn("GH_TOKEN: ${{ github.token }}", JOBS["routing"])
        self.assertIn("needs: routing", JOBS["preflight"])
        self.assertIn(f"    if: ${{{{ {DRAFT_GATE} }}}}\n", JOBS["routing"])
        for job in ("preflight", "dotnet-build", "frontend", "release-pack"):
            self.assertIn(f"    if: ${{{{ {SKIP_GATE} && ({DRAFT_GATE}) }}}}\n", JOBS[job], job)
        self.assertIn(f"    if: ${{{{ {SKIP_GATE} && ({DRAFT_GATE}) && needs.dotnet-build.outputs.has-specs == 'true' }}}}\n",
                      JOBS["specs"])
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
            self.assertIn(f"    if: {SKIP_GATE} && always() && ({DRAFT_GATE})\n", JOBS[job])
        self.assertIn("NEEDS_JSON: ${{ toJson(needs) }}", JOBS["routed-checks"])
        for job in ("routing", "reducer-mongo", "generated-name-matrix", "live-custom-packages",
                    "generated-react", "generated-react-blueprint-templates"):
            self.assertIn(job, JOBS["routed-checks"].splitlines()[0])
        self.assertIn("github.event_name == 'schedule'", JOBS["nightly-red"])
        self.assertIn("needs.build.result != 'success' || needs.routed-checks.result != 'success'", JOBS["nightly-red"])
        self.assertEqual(1, WORKFLOW.count("issues: write"))
        self.assertIn("issues: write", JOBS["nightly-red"])

    def test_every_downstream_job_honors_the_scheduled_skip(self):
        self.assertIn("skip: ${{ steps.already-ran.outputs.skip }}", JOBS["routing"])
        self.assertIn("actions: read", JOBS["routing"])
        self.assertIn("uses: Cratis/Workflows/.github/actions/skip-if-already-ran@main", JOBS["routing"])
        self.assertTrue(JOBS)
        for job in JOBS:
            if job != "routing":
                gate = JOBS[job].split("runs-on:")[0]
                self.assertIn("routing", gate, job)
                self.assertIn(SKIP_GATE, gate, job)

    def test_every_job_gates_draft_pull_requests_without_skipping_other_events(self):
        self.assertTrue(JOBS)
        # nightly-red is not a pull request job: it only runs on schedule or the manual alert dry run.
        for job, definition in JOBS.items():
            if job == "nightly-red":
                continue
            with self.subTest(job=job):
                self.assertIn(DRAFT_GATE, definition)

    def test_nightly_alert_runs_only_on_red_schedule_or_manual_dry_run(self):
        job = JOBS["nightly-red"]
        condition = f"""    if: >-
      {SKIP_GATE} && always() && ((github.event_name == 'schedule' && (needs.build.result != 'success' || needs.routed-checks.result != 'success'))
      || (github.event_name == 'workflow_dispatch' && inputs.test-nightly-alert))
"""
        self.assertIn(condition, job)
        self.assertNotIn("pull_request", job)
        self.assertTrue(job.startswith("    needs: [routing, build, routed-checks]\n"))
        self.assertIn("test-nightly-alert:", WORKFLOW.split("jobs:\n", 1)[0])
        self.assertIn("NIGHTLY_DRY_RUN: ${{ github.event_name == 'workflow_dispatch' && 'true' || 'false' }}", job)

    def test_frontend_and_release_pack_run_beside_the_debug_build_and_stay_required(self):
        for job in ("frontend", "release-pack"):
            self.assertIn("    needs: [preflight, routing]\n", JOBS[job], job)
        moved = {
            "frontend": ("npm ci --prefix Source/Frontend", "npm test --prefix Source/Frontend",
                         "npm run build --prefix Source/Frontend", "verify-stage-entrypoint.py"),
            "release-pack": ("dotnet build --configuration Release -p:Version=9999.0.0", "dotnet pack --no-build",
                             "verify-packed-reducer-consumer.py"),
        }
        for job, fragments in moved.items():
            for fragment in fragments:
                self.assertEqual(1, WORKFLOW.count(fragment), fragment)
                self.assertIn(fragment, JOBS[job], (job, fragment))
                self.assertNotIn(fragment, JOBS["dotnet-build"], fragment)
        self.assertNotIn("setup-node", JOBS["dotnet-build"])
        self.assertIn("mode: restore", JOBS["release-pack"])
        needs = JOBS["build"].splitlines()[0]
        for job in ("frontend", "release-pack"):
            self.assertIn(job, needs)
            self.assertNotIn(job, JOBS["specs"].split("runs-on:")[0])

    def test_heavy_lanes_need_the_build_but_run_in_parallel_with_specs(self):
        live_packages_gate = f"""    needs: [preflight, routing, dotnet-build]
    # Pull requests start heavy lanes after the build passes, in parallel with the spec shards; nightly/manual runs keep full coverage.
    if: >-
      {SKIP_GATE} && !cancelled() && ({DRAFT_GATE}) && needs.preflight.result == 'success' && needs.routing.result == 'success'
      && (github.event_name != 'pull_request' || needs.dotnet-build.result == 'success')"""
        # Lanes that reuse the Debug build output cannot run without it, on any trigger.
        gate = f"""    needs: [preflight, routing, dotnet-build]
    # Lanes reuse the Debug build output, so they need a passing build; they run in parallel with the spec shards.
    if: >-
      {SKIP_GATE} && !cancelled() && ({DRAFT_GATE}) && needs.preflight.result == 'success' && needs.routing.result == 'success'
      && needs.dotnet-build.result == 'success'"""
        standalone_gate = f"""    needs: [preflight, routing, dotnet-build]
    # Unlike the other heavy lanes, this one starts as soon as the build passes, next to the spec shards: it is
    # part of the required `build` aggregate, so waiting for the shards first put both on the critical path.
    if: >-
      {SKIP_GATE} && !cancelled() && ({DRAFT_GATE}) && needs.preflight.result == 'success' && needs.routing.result == 'success'
      && needs.dotnet-build.result == 'success'"""
        heavy = ALL - {"live-blueprints", "standalone-generated-application"}
        self.assertEqual(5, len(heavy))
        self.assertIn("live-custom-packages", heavy)
        self.assertTrue(JOBS["live-custom-packages"].startswith(live_packages_gate), "live-custom-packages")
        for job in heavy - {"live-custom-packages"}:
            self.assertTrue(JOBS[job].startswith(gate), job)
        for job in heavy:
            self.assertNotIn("specs", JOBS[job].split("runs-on:")[0].replace("spec shards", ""), job)
        # Coverage is unchanged: the aggregates still require the spec shards and every routed lane.
        self.assertIn("specs", JOBS["build"].splitlines()[0])
        # The build aggregate passes a reduced needs object: the full context exceeds the environment limit.
        self.assertNotIn("toJson(needs)", JOBS["build"])
        for job in ("preflight", "dotnet-build", "frontend", "release-pack", "specs", "standalone-generated-application"):
            self.assertIn(f'"{job}": {{"result": "${{{{ needs.{job}.result }}}}"}}', JOBS["build"])
        self.assertIn('"routing": ${{ toJson(needs.routing) }}}', JOBS["build"])
        self.assertIn("run: python3 .github/ci-routing.py check-build", JOBS["build"])
        self.assertTrue(JOBS["routed-checks"].startswith("    needs: [routing, reducer-mongo, generated-name-matrix, live-custom-packages, generated-react, generated-react-blueprint-templates]\n"))
        self.assertTrue(JOBS["generated-react"].startswith(gate + "\n      && needs.routing.outputs.generated-react == 'true'\n    runs-on:"), "generated-react")
        self.assertTrue(
            JOBS["standalone-generated-application"].startswith(standalone_gate + "\n      && needs.routing.outputs.standalone-generated-application == 'true'\n    runs-on:"),
            "standalone-generated-application")
        self.assertIn("standalone-generated-application", JOBS["build"].splitlines()[0])
        self.assertIn("check-build", JOBS["build"])

    def test_dry_run_table_covers_requested_scenarios(self):
        with contextlib.redirect_stdout(io.StringIO()) as output:
            CI["dry_run_table"]()
        self.assertEqual(13, len(output.getvalue().splitlines()))
        self.assertIn("ci:full | yes | yes | yes | yes | yes", output.getvalue())
        self.assertIn("Source/Rendering.Cratis only | yes | yes | yes | yes | yes", output.getvalue())


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
        for job in ROUTED - {"live-blueprints"} | {"routing"}:
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
        for lane in ROUTED:
            changed = copy.deepcopy(needs)
            changed["routing"]["outputs"][lane] = "true"
            with self.subTest(lane=lane), self.assertRaises(ValueError):
                CI["require_success"](changed)

    def test_missing_or_invalid_output_fails_closed_even_with_successful_jobs(self):
        for lane in ROUTED:
            for value in (None, "", "TRUE", "skipped"):
                needs = self.needs(ALL)
                needs["routing"]["outputs"][lane] = value
                with self.subTest(lane=lane, value=value), self.assertRaises(ValueError):
                    CI["require_success"](needs)

    def test_unselected_routed_lanes_cannot_hide_failure_or_cancellation(self):
        for lane in ROUTED - {"live-blueprints"}:
            for result in ("failure", "cancelled", "", None):
                needs = self.needs(set())
                needs[lane]["result"] = result
                with self.subTest(lane=lane, result=result), self.assertRaises(ValueError):
                    CI["require_success"](needs)

    def build_needs(self):
        needs = self.needs(ALL)
        needs.update({job: {"result": "success"} for job in ("preflight", "dotnet-build", "frontend", "release-pack", "specs")})
        return needs

    def test_build_permits_success_and_explicitly_unselected_standalone_skip(self):
        for selected, result in (("true", "success"), ("false", "success"), ("false", "skipped")):
            needs = self.build_needs()
            needs["routing"]["outputs"]["standalone-generated-application"] = selected
            needs["standalone-generated-application"]["result"] = result
            CI["require_build"](needs)

    def test_build_rejects_missing_routing_and_unexpected_standalone_outcomes(self):
        for selected, result in (("true", "skipped"), (None, "success"), ("", "success"),
                                 ("false", "failure"), ("false", "cancelled")):
            needs = self.build_needs()
            needs["routing"]["outputs"]["standalone-generated-application"] = selected
            needs["standalone-generated-application"]["result"] = result
            with self.subTest(selected=selected, result=result), self.assertRaises(ValueError):
                CI["require_build"](needs)
        needs = self.build_needs()
        del needs["routing"]["outputs"]["standalone-generated-application"]
        with self.assertRaises(ValueError):
            CI["require_build"](needs)

    def test_build_requires_core_jobs_and_routing(self):
        for job in ("preflight", "dotnet-build", "frontend", "release-pack", "specs", "routing"):
            for result in ("failure", "skipped", "cancelled", ""):
                needs = self.build_needs()
                needs[job]["result"] = result
                with self.subTest(job=job, result=result), self.assertRaises(ValueError):
                    CI["require_build"](needs)

    def test_build_accepts_skipped_specs_only_with_an_explicit_unselected_route(self):
        for selected, result, accepted in (
                ("false", "skipped", True), ("false", "success", True), ("true", "success", True),
                ("true", "skipped", False), (None, "skipped", False), ("", "skipped", False),
                ("TRUE", "skipped", False), ("false", "failure", False), ("false", "cancelled", False),
                ("true", "failure", False), ("true", "cancelled", False), ("false", "", False)):
            needs = self.build_needs()
            needs["specs"]["result"] = result
            if selected is None:
                needs["routing"]["outputs"].pop("specs-selected", None)
            else:
                needs["routing"]["outputs"]["specs-selected"] = selected
            with self.subTest(selected=selected, result=result):
                if accepted:
                    CI["require_build"](needs)
                else:
                    with self.assertRaises(ValueError):
                        CI["require_build"](needs)

    def test_build_requires_the_build_even_when_specs_are_unselected(self):
        for job in ("preflight", "dotnet-build", "frontend", "release-pack"):
            needs = self.build_needs()
            needs["routing"]["outputs"]["specs-selected"] = "false"
            needs["specs"]["result"] = "skipped"
            needs[job]["result"] = "skipped"
            with self.subTest(job=job), self.assertRaises(ValueError):
                CI["require_build"](needs)

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


class ReducerMongoMatrixTests(unittest.TestCase):
    FILTERS = {
        "runtime-proofs": '"--filter", "|".join("FullyQualifiedName=" + name for name in sorted(expected))',
        "rendering-probes": "|".join("FullyQualifiedName~" + name for name in (
            "when_verifying_billing_mongo_deletion.should_match_model_bound_read_model_query_in_mongo_container_when_configured",
            "when_verifying_scoped_projections_against_mongo.should_report_both_mongo_shapes_when_configured",
            "when_verifying_routed_appends_against_mongo.should_append_all_routes_when_configured",
            "when_verifying_reducer_filters_against_mongo.should_filter_reducer_appends_when_configured",
            "when_verifying_reactions_against_the_pinned_kernel.should_settle_event_reactions_when_configured",
            "when_verifying_reducer_migration_against_the_pinned_kernel.should_keep_one_builder_with_the_previous_observer_id_when_configured",
            "when_verifying_inline_code_against_the_pinned_kernel.should_verify_demo_when_configured",
            "when_verifying_inline_code_against_the_pinned_kernel.should_verify_min_a3_when_configured",
            "when_verifying_application_trigger_reactions_against_the_pinned_kernel.should_append_once_per_namespace_and_skip_a_receipted_redelivery_when_configured",
            "when_verifying_application_trigger_invocations_against_the_pinned_kernel.should_resume_commands_and_retain_refusals_when_configured",
            "when_verifying_reaction_bodies_against_the_pinned_kernel.should_settle_native_reaction_bodies_when_configured",
        )),
        "reconnect": (
            "FullyQualifiedName~when_reconnecting_against_the_pinned_kernel.should_keep_stage_projections_updating_when_configured"
            "|FullyQualifiedName~when_encrypting_against_the_pinned_kernel.should_encrypt_and_release_concepts_when_configured"
        ),
    }

    def validation_script(self):
        step = JOBS["reducer-mongo"].split("      - name: Require all live probes to execute and pass\n", 1)[1]
        return textwrap.dedent(step.split("          python3 - <<'PY'\n", 1)[1].split("          PY\n", 1)[0])

    def test_original_filters_run_once_on_separate_legs_without_rebuilding(self):
        job = JOBS["reducer-mongo"]
        matrix = re.search(r"^        probe: \[(.+)\]$", job, re.MULTILINE)[1].split(", ")
        self.assertCountEqual(self.FILTERS, matrix)
        self.assertCountEqual(CI["REDUCER_MONGO_PROBES"], matrix)
        self.assertIn("fail-fast: false", job)
        test_steps = [step for step in job.split("      - name: ")[1:] if "--filter" in step]
        self.assertEqual(3, len(test_steps))
        for probe, expression in self.FILTERS.items():
            with self.subTest(probe=probe):
                step, = [step for step in test_steps if expression in step]
                self.assertEqual(1, step.count(expression))
                self.assertIn(f"if: matrix.probe == '{probe}'", step)
                self.assertIn("--no-build", step)
                self.assertIn("net11.0", step)
        self.assertIn("name: reducer-mongo (${{ matrix.probe }})", job)
        self.assertIn("-${{ matrix.probe }}", job.split("REDUCER_MONGO_CONTAINER:", 1)[1].splitlines()[0])
        artifacts = re.findall(r"^          name: (reducer-mongo-.+)$", job, re.MULTILINE)
        self.assertEqual(3, len(artifacts))
        self.assertTrue(all(name.endswith("-${{ matrix.probe }}") for name in artifacts))
        self.assertIn("reducer-mongo", JOBS["routed-checks"].splitlines()[0])
        self.assertIn("NEEDS_JSON: ${{ toJson(needs) }}", JOBS["routed-checks"])
        self.assertEqual((CI["RENDERING_PROJECT"], CI["HOST_PROJECT"]), CI["LANE_BUILD_ROOTS"]["reducer-mongo"])

    def expected_names(self, probe):
        # Evaluate only the expected-name construction, not the result checks.
        namespace = {}
        with patch.dict(os.environ, PROBE=probe), patch.object(ET, "parse", return_value=ET.ElementTree(ET.Element("TestRun"))):
            exec(self.validation_script().split("counters =", 1)[0], namespace)
        return namespace["expected"]

    def validate_results(self, probe, names, outcome="Passed"):
        ns = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
        root = ET.Element(ns + "TestRun")
        definitions = ET.SubElement(root, ns + "TestDefinitions")
        results = ET.SubElement(root, ns + "Results")
        for index, name in enumerate(names):
            class_name, method = name.rsplit(".", 1)
            unit = ET.SubElement(definitions, ns + "UnitTest", id=str(index))
            ET.SubElement(unit, ns + "TestMethod", className=class_name, name=method)
            ET.SubElement(results, ns + "UnitTestResult", testId=str(index), outcome=outcome)
        summary = ET.SubElement(root, ns + "ResultSummary")
        ET.SubElement(summary, ns + "Counters", executed=str(len(names)))
        filename = {"rendering-probes": "billing-mongo.trx", "reconnect": "stage-reconnect.trx"}[probe]
        with patch.dict(os.environ, PROBE=probe), patch.object(ET, "parse", return_value=ET.ElementTree(root)) as parse:
            exec(self.validation_script(), {})
        parse.assert_called_once_with("TestResults/reducer-mongo/" + filename)

    def test_each_persistence_leg_requires_exactly_its_original_tests(self):
        rendering = self.expected_names("rendering-probes")
        reconnect = self.expected_names("reconnect")
        self.assertEqual(11, len(rendering))
        self.assertEqual(2, len(reconnect))
        self.assertFalse(rendering & reconnect)
        for probe, names in (("rendering-probes", rendering), ("reconnect", reconnect)):
            with self.subTest(probe=probe):
                self.validate_results(probe, names)
                self.assertTrue(all(name.startswith("Cratis.Stage.Host.") == (probe == "reconnect") for name in names))
                self.assertIn("if: matrix.probe != 'runtime-proofs'", JOBS["reducer-mongo"])
                for invalid in (list(names)[:-1], list(names) + list(names)[:1], rendering | reconnect):
                    with self.assertRaises(SystemExit):
                        self.validate_results(probe, invalid)
                with self.assertRaises(SystemExit):
                    self.validate_results(probe, names, outcome="NotExecuted")


def build_plan(*paths, event="pull_request", labels=(), lanes=None):
    return CI["select_build"](paths, event, labels, lanes if lanes is not None else set())


class AffectedBuildTests(unittest.TestCase):
    ROOTS = {"Source/Rendering.Cratis/Rendering.Cratis.csproj"}
    EVERYTHING = set(CI["solution_projects"]())
    EVERY_SHARD = {shard["name"] for shard in CI["spec_matrix"]()}

    def assert_full(self, plan):
        self.assertTrue(plan.full)
        self.assertEqual(self.EVERYTHING, set(plan.projects))
        self.assertEqual(self.EVERYTHING, set(plan.affected))
        self.assertEqual(self.EVERY_SHARD, set(plan.shards))

    def test_documentation_builds_only_lane_roots_and_runs_no_specs(self):
        plan = build_plan("Documentation/guide.md", "README.md", lanes={"generated-react"})
        self.assertFalse(plan.full)
        self.assertEqual((), plan.affected)
        self.assertEqual(tuple(self.ROOTS), plan.projects)
        self.assertEqual((), plan.shards)
        self.assertEqual((), build_plan("README.md").projects)

    def test_a_leaf_project_runs_only_its_shard_and_its_own_projects(self):
        plan = build_plan("Source/Extensions/DotNET/Foo.cs")
        extensions = "Source/Extensions/DotNET/Cratis.Stage.Extensions.csproj"
        self.assertIn(extensions, plan.affected)
        self.assertEqual({"small-projects"}, set(plan.shards))
        self.assertLess(len(plan.affected), len(self.EVERYTHING))
        self.assertNotIn("Source/Rendering.Cratis/Rendering.Cratis.csproj", plan.projects)
        narrowed = CI_SHARDS["narrow"](CI_SHARDS["matrix"](), plan.affected)["include"]
        self.assertEqual(["small-projects"], [shard["name"] for shard in narrowed])
        self.assertIn(extensions, narrowed[0]["projects"])
        self.assertNotIn("Source/Host/Host.csproj", narrowed[0]["projects"])

    def test_the_renderer_reaches_its_dependents_and_every_shard_of_sharded_projects(self):
        plan = build_plan("Source/Rendering.Cratis/X.cs")
        for project in ("Source/Rendering.Cratis/Rendering.Cratis.csproj", "Source/Rendering.Cratis.Scaffolding/Rendering.Cratis.Scaffolding.csproj",
                        "Source/Host/Host.csproj", "Source/BodyWorker/BodyWorker.csproj", CI["CONFORMANCE_PROJECT"]):
            self.assertIn(project, plan.affected)
        for prefix in ("rendering-", "conformance-"):
            expected = {name for name in self.EVERY_SHARD if name.startswith(prefix)}
            self.assertTrue(expected <= set(plan.shards), prefix)

    def test_stage_changes_reach_the_host_through_its_nested_publish(self):
        self.assertIn(CI["HOST_PROJECT"], build_plan("Source/Stage/X.cs").affected)

    def test_lane_roots_are_built_for_selected_lanes_and_cover_every_lane(self):
        self.assertEqual(set(CI["LANE_PATHS"]), set(CI["LANE_BUILD_ROOTS"]))
        plan = build_plan("Source/Extensions/DotNET/Foo.cs", lanes={"reducer-mongo", "generated-name-matrix"})
        for project in (CI["RENDERING_PROJECT"], CI["HOST_PROJECT"], CI["CONFORMANCE_PROJECT"]):
            self.assertIn(project, plan.projects)
            self.assertNotIn(project, plan.affected)
        self.assertTrue(set(plan.affected) <= set(plan.projects) <= self.EVERYTHING)

    def test_shared_build_inputs_workflows_and_the_routing_itself_fall_back_to_full(self):
        for path in ("Directory.Build.props", "Source/Directory.Build.props", "Source/Directory.Build.targets", "Directory.Packages.props",
                     "global.json", "NuGet.config", "Source/Host/nuget.config", "Source/Host/packages.lock.json", "Stage.slnx",
                     ".github/workflows/x.yml", ".github/ci-routing.py", ".github/test_ci_routing.py", ".github/spec-shards.py",
                     ".github/spec-shard-timings.json"):
            with self.subTest(path=path):
                self.assert_full(build_plan("Documentation/guide.md", path))

    def test_unmapped_and_disk_read_paths_fall_back_to_full(self):
        for path in ("Foo/bar.txt", "Source/NewProject/X.cs", "Samples/Billing/Ledger/Payments/Payments.play",
                     "Verification/live-navigation/model/x.play", "Source/Frontend/Runtime/package.json"):
            with self.subTest(path=path):
                self.assert_full(build_plan("Source/Extensions/DotNET/Foo.cs", path))

    def test_a_project_folder_document_is_an_input_not_a_harmless_doc(self):
        plan = build_plan("Source/Extensions/DotNET/Notes.md")
        self.assertIn("Source/Extensions/DotNET/Cratis.Stage.Extensions.csproj", plan.affected)

    def test_every_event_other_than_a_plain_pull_request_is_full(self):
        for event, labels in (("schedule", ()), ("workflow_dispatch", ()), ("pull_request", ("ci:full",))):
            with self.subTest(event=event):
                self.assert_full(build_plan("Documentation/guide.md", event=event, labels=labels))
        with self.assertRaises(ValueError):
            build_plan("README.md", event="push")

    def test_any_error_computing_the_plan_fails_closed_to_full(self):
        with patch.dict(CI, {}), contextlib.redirect_stdout(io.StringIO()):
            original = CI["affected_by"].__globals__["project_inputs"]
            CI["affected_by"].__globals__["project_inputs"] = lambda *args, **kwargs: (_ for _ in ()).throw(OSError("boom"))
            try:
                self.assert_full(build_plan("Source/Extensions/DotNET/Foo.cs"))
            finally:
                CI["affected_by"].__globals__["project_inputs"] = original

    def test_a_lane_root_missing_from_the_solution_fails_closed_to_full(self):
        roots = CI["LANE_BUILD_ROOTS"]
        with patch.dict(roots, {"generated-react": ("Source/Gone/Gone.csproj",)}), contextlib.redirect_stdout(io.StringIO()):
            self.assert_full(build_plan("README.md", lanes={"generated-react"}))

    def test_both_sides_of_a_rename_count(self):
        with patch("subprocess.check_output", side_effect=["base", "Source/Stage/A.cs\0Source/Extensions/DotNET/A.cs\0", '{"labels": []}']), \
                patch.dict(os.environ, GITHUB_REPOSITORY="Cratis/Stage"):
            _, plan = CI["github_plan"]("pull_request", {"number": 1, "pull_request": {"base": {"ref": "main"}}})
        self.assertIn(CI["HOST_PROJECT"], plan.affected)
        self.assertIn("Source/Extensions/DotNET/Cratis.Stage.Extensions.csproj", plan.affected)

    def test_the_full_plan_matches_the_unnarrowed_matrix_and_narrowing_never_invents_shards(self):
        self.assertEqual(CI["spec_matrix"](), CI_SHARDS["matrix"]()["include"])
        self.assertEqual(CI_SHARDS["matrix"](), CI_SHARDS["narrow"](CI_SHARDS["matrix"](), self.EVERYTHING))
        for paths in (["README.md"], ["Source/Extensions/DotNET/A.cs"], ["Source/Host/A.cs"]):
            self.assertTrue(set(build_plan(*paths).shards) <= self.EVERY_SHARD)

    def test_routes_write_the_build_outputs(self):
        output = mock_open()
        plan = build_plan("Documentation/guide.md")
        with patch.dict(os.environ, GITHUB_OUTPUT="unused"), patch("builtins.open", output), contextlib.redirect_stdout(io.StringIO()):
            CI["write_routes"](set(), plan)
        written = "".join(call.args[0] for call in output().write.call_args_list)
        for expected in ("build-scope=affected\n", "specs-selected=false\n", "spec-shards=[]\n", "affected-projects=[]\n"):
            self.assertIn(expected, written)
        self.assertIn("build-projects=[", written)

    def test_solution_filter_lists_only_solution_projects(self):
        with contextlib.redirect_stdout(io.StringIO()) as output:
            CI["write_solution_filter"](json.dumps([CI["HOST_PROJECT"]]))
        self.assertEqual([CI["HOST_PROJECT"]], json.loads(output.getvalue())["solution"]["projects"])
        for bad in ([], ["Source/Gone/Gone.csproj"]):
            with self.subTest(bad=bad), self.assertRaises(ValueError):
                CI["write_solution_filter"](json.dumps(bad))

    def test_workflow_builds_the_filter_only_for_affected_scope_and_keeps_check_names(self):
        build = JOBS["dotnet-build"]
        self.assertIn("dotnet build --configuration Debug\n", build)
        self.assertIn('if [ "$BUILD_SCOPE" = affected ]', build)
        self.assertIn('dotnet build "$RUNNER_TEMP/affected.slnf" --configuration Debug', build)
        self.assertIn('args+=(--affected "$AFFECTED_PROJECTS")', build)
        self.assertIn("has-specs: ${{ steps.spec-coverage.outputs.has-specs }}", build)
        for job in ("routing", "preflight", "dotnet-build", "frontend", "release-pack", "specs", "build", *CI["LANE_PATHS"]):
            self.assertIn(job if job != "live-blueprints" else "live-custom-packages", JOBS)
        self.assertIn("name: specs (${{ matrix.name }})", JOBS["specs"])


class PublishGateTests(unittest.TestCase):
    sha = "a" * 40

    def run_record(self):
        return dict(id=42, run_attempt=1, event="workflow_dispatch", head_sha=self.sha, head_branch="main",
                    path=".github/workflows/dotnet-build.yml", status="completed", conclusion="success",
                    html_url="https://github.com/Cratis/Stage/actions/runs/42")

    def jobs(self):
        names = (ALL - {"live-blueprints", "reducer-mongo"}) | {"routing", "build", "routed-checks"} | {
            "reducer-mongo (runtime-proofs)", "reducer-mongo (rendering-probes)", "reducer-mongo (reconnect)"}
        jobs = [{"name": name, "conclusion": "success", "steps": []} for name in sorted(names)]
        for job in jobs:
            if job["name"] == "live-custom-packages":
                job["steps"] = [{"name": name, "conclusion": "success"} for name in CI["LIVE_STEPS"]]
        return jobs

    def test_full_proof_requires_exact_revision_and_every_lane_and_live_probe(self):
        run, jobs = self.run_record(), self.jobs()
        for event in ("push", "schedule", "workflow_dispatch"):
            self.assertTrue(CI["full_run_succeeded"]({**run, "event": event}, self.sha, jobs))
        for key, value in (("head_sha", "b" * 40), ("event", "pull_request"), ("event", "pull_request_target"),
                           ("head_branch", "feature"), ("path", ".github/workflows/fake.yml"), ("path", None),
                           ("conclusion", "failure"), ("conclusion", "cancelled"), ("status", "in_progress")):
            with self.subTest(key=key, value=value):
                self.assertFalse(CI["full_run_succeeded"]({**run, key: value}, self.sha, jobs))
        for index, job in enumerate(jobs):
            for result in ("skipped", "failure", "cancelled", None):
                changed = copy.deepcopy(jobs)
                changed[index]["conclusion"] = result
                with self.subTest(job=job["name"], result=result):
                    self.assertFalse(CI["full_run_succeeded"](run, self.sha, changed))
            self.assertFalse(CI["full_run_succeeded"](run, self.sha, jobs[:index] + jobs[index + 1:]))
        for name in CI["LIVE_STEPS"]:
            changed = copy.deepcopy(jobs)
            live = next(job for job in changed if job["name"] == "live-custom-packages")
            next(step for step in live["steps"] if step["name"] == name)["conclusion"] = "skipped"
            self.assertFalse(CI["full_run_succeeded"](run, self.sha, changed))

    def test_legacy_reducer_job_cannot_replace_all_or_any_matrix_leg(self):
        jobs = self.jobs()
        legacy = {"name": "reducer-mongo", "conclusion": "success", "steps": []}
        matrix = [job for job in jobs if job["name"].startswith("reducer-mongo (")]
        self.assertEqual(3, len(matrix))
        singles = [job for job in jobs if job not in matrix]
        self.assertFalse(CI["full_run_succeeded"](self.run_record(), self.sha, singles + [legacy]))
        for leg in matrix:
            with self.subTest(leg=leg["name"]):
                missing = [job for job in jobs if job != leg]
                self.assertFalse(CI["full_run_succeeded"](self.run_record(), self.sha, missing + [legacy]))

    def test_failed_duplicate_matrix_job_cannot_hide_behind_success(self):
        jobs = self.jobs()
        for probe in ("runtime-proofs", "rendering-probes", "reconnect"):
            failed = {"name": f"reducer-mongo ({probe})", "conclusion": "failure"}
            for changed in ([failed] + jobs, jobs + [failed]):
                with self.subTest(probe=probe, changed=changed):
                    self.assertFalse(CI["full_run_succeeded"](self.run_record(), self.sha, changed))

    def test_pr_is_rejected_even_with_exact_sha_and_faked_full_job_and_step_names(self):
        run = {**self.run_record(), "event": "pull_request"}
        jobs = self.jobs()
        next(job for job in jobs if job["name"] == "routing")["steps"] = [
            {"name": f"Full routing for {self.sha}", "conclusion": "success"}]
        self.assertFalse(CI["full_run_candidate"](run, self.sha))
        self.assertFalse(CI["full_run_succeeded"](run, self.sha, jobs))

    def gate(self, responses):
        with patch.dict(os.environ, GITHUB_REPOSITORY="Cratis/Stage", GITHUB_SHA=self.sha), \
                patch("subprocess.check_output", side_effect=responses) as command, \
                patch("time.sleep"), contextlib.redirect_stdout(io.StringIO()):
            CI["publish_gate"]()
            return command

    def test_missing_run_provenance_fails_closed(self):
        for key in ("head_branch", "head_sha", "path", "event", "status", "conclusion"):
            run = self.run_record()
            del run[key]
            with self.subTest(key=key):
                self.assertFalse(CI["full_run_succeeded"](run, self.sha, self.jobs()))

    def test_untrusted_run_cannot_be_reused_or_fetch_jobs_before_dispatch(self):
        for key, value in (("event", "pull_request"), ("head_branch", "feature"),
                           ("head_sha", "b" * 40), ("path", ".github/workflows/fake.yml")):
            untrusted = {**self.run_record(), key: value}
            complete = {**self.run_record(), "id": 43}
            command = self.gate([json.dumps({"workflow_runs": [untrusted]}),
                                 json.dumps({"object": {"sha": self.sha}}), "",
                                 json.dumps({"workflow_runs": [untrusted, complete]}),
                                 json.dumps([{"jobs": self.jobs()}])])
            with self.subTest(key=key, value=value):
                self.assertIn("git/ref/heads/main", command.call_args_list[1].args[0][-1])
                self.assertIn("force-full=true", command.call_args_list[2].args[0])
                self.assertIn("/runs/43/", command.call_args_list[4].args[0][-1])
                self.assertEqual(5, command.call_count)

    def test_reuses_full_ci_without_dispatching(self):
        command = self.gate([json.dumps({"workflow_runs": [self.run_record()]}), json.dumps([{"jobs": self.jobs()}])])
        self.assertEqual(2, command.call_count)
        self.assertIn("/attempts/1/jobs?per_page=100", command.call_args_list[1].args[0][-1])

    def test_narrow_green_dispatches_full_and_waits_for_same_sha(self):
        narrow = self.jobs()
        next(job for job in narrow if job["name"] == "standalone-generated-application")["conclusion"] = "skipped"
        old = self.run_record()
        running = {**old, "id": 43, "status": "in_progress", "conclusion": None}
        complete = {**running, "status": "completed", "conclusion": "success"}
        newer_main = {**complete, "id": 44, "head_sha": "b" * 40}
        command = self.gate([json.dumps({"workflow_runs": [old]}), json.dumps([{"jobs": narrow}]),
                             json.dumps({"object": {"sha": self.sha}}), "",
                             json.dumps({"workflow_runs": [old, newer_main, running]}),
                             json.dumps({"workflow_runs": [old, newer_main, complete]}), json.dumps([{"jobs": self.jobs()}])])
        self.assertIn("force-full=true", command.call_args_list[3].args[0])
        self.assertIn("--ref", command.call_args_list[3].args[0])
        self.assertEqual(7, command.call_count)
        self.assertIn("/runs/43/", command.call_args_list[6].args[0][-1])

    def test_moved_main_and_api_errors_fail_closed(self):
        with self.assertRaises(ValueError):
            self.gate([json.dumps({"workflow_runs": []}), json.dumps({"object": {"sha": "b" * 40}})])
        for error in (RuntimeError("API unavailable"), ValueError("bad JSON")):
            with self.subTest(error=error), self.assertRaises(type(error)):
                self.gate([error])
        with self.assertRaises(json.JSONDecodeError):
            self.gate(["not JSON"])

    def test_failed_cancelled_or_incomplete_dispatched_run_blocks_publish(self):
        for result in ("failure", "cancelled", "success"):
            run = {**self.run_record(), "conclusion": result}
            responses = [json.dumps({"workflow_runs": []}), json.dumps({"object": {"sha": self.sha}}), "",
                         json.dumps({"workflow_runs": [run]})]
            if result == "success":
                responses.append(json.dumps([{"jobs": self.jobs()[:-1]}]))
            with self.subTest(result=result), self.assertRaises(ValueError):
                self.gate(responses)

    def test_wait_is_bounded_and_never_accepts_newer_main(self):
        for running in ({**self.run_record(), "status": "queued", "conclusion": None},
                        {**self.run_record(), "head_sha": "b" * 40},
                        {**self.run_record(), "head_branch": "feature"},
                        {**self.run_record(), "path": ".github/workflows/fake.yml"},
                        {**self.run_record(), "event": "pull_request"}):
            clock = [0]
            initial = iter([json.dumps({"workflow_runs": []}), json.dumps({"object": {"sha": self.sha}}), ""])
            def response(*args, **kwargs):
                return next(initial, json.dumps({"workflow_runs": [running]}))
            def sleep(seconds):
                clock[0] += seconds
            with patch.dict(os.environ, GITHUB_REPOSITORY="Cratis/Stage", GITHUB_SHA=self.sha), \
                    patch("subprocess.check_output", side_effect=response), \
                    patch("time.monotonic", side_effect=lambda: clock[0]), patch("time.sleep", side_effect=sleep), \
                    self.assertRaises(TimeoutError):
                CI["publish_gate"]()
            self.assertEqual(55 * 60, clock[0])

    def test_publish_blocks_release_before_mutation_and_uses_default_off_switch(self):
        workflow = Path(__file__).with_name("workflows").joinpath("publish.yml").read_text()
        gate = workflow.split("  full-ci:\n", 1)[1].split("  release:\n", 1)[0]
        self.assertEqual(2, gate.count("if: vars.STAGE_ROUTE_ALWAYS_ON_LANES == 'true'"))
        self.assertIn("actions: write", gate)
        self.assertIn("GH_TOKEN: ${{ github.token }}", gate)
        self.assertIn("timeout-minutes: 60", gate)
        self.assertIn("python3 .github/ci-routing.py publish-gate", gate)
        self.assertIn("  release:\n    needs: full-ci\n", workflow)
        self.assertIn("STAGE_ROUTE_ALWAYS_ON_LANES: ${{ vars.STAGE_ROUTE_ALWAYS_ON_LANES }}", JOBS["routing"])
        self.assertNotIn("Full routing for", JOBS["routing"])
        self.assertNotIn("full-sha", JOBS["routing"])
        self.assertIn("force-full:", WORKFLOW)


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

    def test_missing_label_with_empty_gh_output_is_created(self):
        # gh prints nothing (not []) for an empty `label list --json`; json.loads("") broke the alert (#632).
        calls = self.report([json.dumps({"workflow_runs": []}), "[]", "", "", ""])
        self.assertEqual(5, len(calls))
        self.assertEqual(("label", "create", "ci-nightly-red"), calls[-2].args[:3])
        self.assertEqual(("issue", "create"), calls[-1].args[:2])

    def test_dry_run_reads_but_never_writes(self):
        with patch.dict(os.environ, NIGHTLY_DRY_RUN="true"), contextlib.redirect_stdout(io.StringIO()) as output:
            calls = self.report([json.dumps({"workflow_runs": []}), ""])
        self.assertEqual(2, len(calls))
        self.assertEqual({"api", "issue"}, {call.args[0] for call in calls})
        self.assertNotIn("create", {call.args[1] for call in calls})
        self.assertIn("Dry run", output.getvalue())

    def test_api_failure_propagates_without_retry_or_issue_mutation(self):
        with self.assertRaises(RuntimeError):
            self.report([RuntimeError("GitHub unavailable")])


if __name__ == "__main__":
    unittest.main()
