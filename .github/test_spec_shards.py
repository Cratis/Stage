#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fast checks for lossless shard routing and fail-closed CI guards."""

from collections import Counter
import contextlib
import io
from itertools import product
import json
from pathlib import Path
import re
import runpy
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

SHARDS = runpy.run_path(str(Path(__file__).with_name("spec-shards.py")))


def project_test_status(command, **kwargs):
    stdout = "false\n" if command[2] == "Source/BodyWorker/BodyWorker.csproj" else "true\n"
    return subprocess.CompletedProcess(command, 0, stdout=stdout)


class SpecShardsTests(unittest.TestCase):
    def test_every_known_prefix_and_new_namespace_has_exactly_one_shard(self):
        for sharding in SHARDS["SHARDED"]:
            with self.subTest(project=sharding.name):
                positive = SHARDS["partitions"](sharding)[:-1]
                names = [prefix + "should_pass" for prefix in SHARDS["source_groups"](sharding)]
                namespace = sharding.split[0].rsplit(".", 1)[0]
                names.append(namespace + ".for_A_New_Renderer.when_added.should_pass")
                # A new class in a namespace split per class has no prefix of its own yet.
                names.extend(split + ".when_a_new_class_is_added.should_pass" for split in sharding.split)
                for name in names:
                    matches = [any(prefix.lower() in name.lower() for prefix in partition)
                               for partition in positive]
                    self.assertEqual(1, sum(matches + [not any(matches)]), name)

    def test_conformance_splits_every_heavy_class_and_keeps_namespaces_together(self):
        groups = SHARDS["source_groups"](SHARDS["CONFORMANCE_SHARDING"])
        prefixes = [prefix + "." for prefix in (SHARDS["PLANNER"], SHARDS["THREE_WAY"])]
        for split in prefixes:
            with self.subTest(split=split):
                classes = [prefix for prefix in groups if prefix.startswith(split)]
                self.assertGreater(len(classes), 30)
                self.assertTrue(all(prefix.count(".") == split.count(".") + 1 for prefix in classes), classes)
        # A file declaring several test classes contributes every one of them.
        self.assertIn(SHARDS["PLANNER"] + ".when_comparing_v2_corpus_specifications.", groups)
        self.assertIn("Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.", groups)
        self.assertFalse([prefix for prefix in groups if prefix.startswith(
            "Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.") and prefix.count(".") > 5])

    def test_test_classes_are_top_level_declarations_holding_a_test(self):
        text = ("namespace N.for_X;\npublic class when_a(when_a.context fixture)\n{\n    [Fact] void should_a() {}\n"
                "    public class context {}\n}\npublic sealed class Helper : FactAttribute {}\n"
                "public class when_b : Specification\n{\n    [Theory] void should_b(int value) {}\n}\n")
        self.assertEqual(["when_a", "when_b"], SHARDS["test_classes"](text))

    def test_final_filter_negates_every_positive_prefix(self):
        shards = {shard["name"]: shard for shard in SHARDS["matrix"]()["include"]}
        for sharding in SHARDS["SHARDED"]:
            with self.subTest(project=sharding.name):
                partitions = SHARDS["partitions"](sharding)
                expected = SHARDS["EXCLUSIONS"] + "&" + "&".join(
                    "FullyQualifiedName!~" + prefix
                    for partition in partitions[:-1] for prefix in partition)
                self.assertEqual(expected, shards[sharding.name + "-remainder"]["filter"])
        self.assertTrue(all(shard["filter"].startswith(SHARDS["EXCLUSIONS"]) for shard in shards.values()))

    def test_small_projects_shard_runs_exactly_the_eight_merged_projects_with_unchanged_filters(self):
        expected = ["Source/Host/Host.csproj", "Source/Specifications/Specifications.csproj",
                    "Source/Contracts/Contracts.csproj", "Source/Rendering.Kits/Rendering.Kits.csproj",
                    "Source/Rendering.Templates/Rendering.Templates.csproj",
                    "Source/Rendering.Declarative/Rendering.Declarative.csproj", "Source/Stage/Stage.csproj",
                    "Source/SpecRunner/SpecRunner.csproj",
                    "Source/Rendering.Cratis.Scaffolding/Rendering.Cratis.Scaffolding.csproj"]
        for lane in (False, True):
            with self.subTest(blueprint_templates_lane=lane):
                shards = {shard["name"]: shard for shard in SHARDS["matrix"](blueprint_templates_lane=lane)["include"]}
                self.assertNotIn("host", shards)
                self.assertEqual(expected, shards["small-projects"]["projects"])
                filter_ = SHARDS["EXCLUSIONS"] + ("&FullyQualifiedName!~" + SHARDS["BLUEPRINT_TEMPLATES_FIXTURE"] if lane else "")
                self.assertEqual(filter_, shards["small-projects"]["filter"])

    def test_guard_rejects_a_missing_or_duplicated_merged_project(self):
        full = SHARDS["matrix"]()
        merged = next(shard for shard in full["include"] if shard["name"] == "small-projects")
        for label, projects in (("missing", merged["projects"][1:]), ("duplicated", merged["projects"] + merged["projects"][:1])):
            with self.subTest(label):
                shards = {"include": [dict(merged, projects=projects) if shard is merged else shard
                                      for shard in full["include"]]}
                with patch("subprocess.run") as run, self.assertRaises(SystemExit):
                    run.side_effect = project_test_status
                    SHARDS["guard_solution"](shards)

    def test_specifications_is_one_shard_and_conformance_three_disjoint_shards(self):
        shards = SHARDS["matrix"]()["include"]
        by_name = {shard["name"]: shard for shard in shards}
        self.assertEqual(len(shards), len(by_name), "shard names must be unique")
        self.assertNotIn("conformance", by_name)
        self.assertNotIn("specifications", by_name)
        # Whole-project shards carry no positive prefix, so nothing in them can be skipped by a filter.
        self.assertEqual(SHARDS["EXCLUSIONS"], by_name["small-projects"]["filter"])
        conformance = [shard["name"] for shard in shards if SHARDS["CONFORMANCE"] in shard["projects"]]
        self.assertEqual(["conformance-1", "conformance-2", "conformance-remainder"], conformance)
        self.assertTrue(all(by_name[name]["projects"] == [SHARDS["CONFORMANCE"]] for name in conformance))
        projects = Counter(project for shard in shards for project in shard["projects"])
        self.assertEqual(1, projects["Source/Specifications/Specifications.csproj"])
        self.assertEqual(3, projects[SHARDS["CONFORMANCE"]])

    def test_rendering_shards_do_not_depend_on_conformance_sharding(self):
        # The Rendering entries were byte-identical before and after Conformance was sharded.
        rendering_only = (SHARDS["RENDERING_SHARDING"],)
        for lane in (False, True):
            with self.subTest(blueprint_templates_lane=lane):
                with patch.dict(SHARDS["matrix"].__globals__, {"SHARDED": rendering_only}):
                    alone = SHARDS["matrix"](blueprint_templates_lane=lane)["include"]
                both = SHARDS["matrix"](blueprint_templates_lane=lane)["include"]
                rendering = [shard for shard in both if SHARDS["RENDERING"] in shard["projects"]]
                self.assertEqual([shard for shard in alone if SHARDS["RENDERING"] in shard["projects"]], rendering)
                self.assertEqual(["rendering-1", "rendering-2", "rendering-3", "rendering-remainder"],
                                 [shard["name"] for shard in rendering])

    def test_blueprint_templates_run_exactly_once_with_or_without_the_dedicated_lane(self):
        workflow = Path(__file__).with_name("workflows").joinpath("dotnet-build.yml").read_text()
        lane = workflow.split("  generated-react-blueprint-templates:\n", 1)[1]
        dedicated_filter = re.search(r'--filter "([^"]+)"', lane)[1]
        name = SHARDS["NAMESPACE"] + ".for_GeneratedReactApplication." + SHARDS["BLUEPRINT_TEMPLATES_FIXTURE"] + ".should_pass"
        self.assertIn("BLUEPRINT_TEMPLATES_LANE: ${{ needs.routing.outputs.generated-react-blueprint-templates }}", workflow)
        guard_script = re.search(r'          args=\(\)\n.*?          python3 \.github/spec-shards.py guard "\$\{args\[@\]\}"', workflow, re.DOTALL)[0]
        guard_script = guard_script.replace('python3 .github/spec-shards.py guard', 'printf "%s\\n"')
        for dedicated_lane in (False, True):
            with self.subTest(dedicated_lane=dedicated_lane):
                invocation = subprocess.run(["bash", "-c", guard_script], check=True, capture_output=True, text=True,
                                            env={"BLUEPRINT_TEMPLATES_LANE": str(dedicated_lane).lower()})
                self.assertEqual("--blueprint-templates-lane" if dedicated_lane else "", invocation.stdout.strip())
                shards = SHARDS["matrix"](blueprint_templates_lane=dedicated_lane)["include"]
                selected = sum(self.matches_filter(name, shard["filter"])
                               for shard in shards if SHARDS["RENDERING"] in shard["projects"])
                self.assertEqual(0 if dedicated_lane else 1, selected)
                self.assertEqual(1, selected + int(dedicated_lane and self.matches_filter(name, dedicated_filter)))

    def test_standalone_generated_application_runs_only_in_its_gating_job(self):
        jobs = self.workflow_jobs()
        lane = jobs["standalone-generated-application"]
        dedicated_filter = re.search(r'--filter "([^"]+)"', lane)[1]
        name = (SHARDS["NAMESPACE"] + ".for_CratisBackendApplicationScaffold." +
                SHARDS["STANDALONE_APPLICATION_FIXTURE"] + ".should_run_the_generated_specifications_in_release")
        for dedicated_lane in (False, True):
            with self.subTest(dedicated_lane=dedicated_lane):
                shards = SHARDS["matrix"](blueprint_templates_lane=dedicated_lane)["include"]
                self.assertEqual(0, sum(self.matches_filter(name, shard["filter"]) for shard in shards))
        self.assertTrue(self.matches_filter(name, dedicated_filter))
        self.assertIn("Source/Rendering.Cratis/Rendering.Cratis.csproj", lane)
        self.assertIn("timeout-minutes:", lane)
        self.assertIn("spec-shards.py require-executed", lane)
        # Moving the spec out of the shards must not move it out of the aggregate build check.
        self.assertRegex(jobs["build"], r"needs: \[[^\]\n]*\bstandalone-generated-application\b")
        self.assertIn("STANDALONE_RESULT: ${{ needs.standalone-generated-application.result }}", jobs["build"])

    @staticmethod
    def workflow_jobs():
        workflow = Path(__file__).with_name("workflows").joinpath("dotnet-build.yml").read_text()
        sections = re.split(r"^  ([\w-]+):\n", workflow.split("jobs:\n", 1)[1], flags=re.MULTILINE)
        return dict(zip(sections[1::2], sections[2::2]))

    @staticmethod
    def matches_filter(name, expression):
        # Shard filters are an AND of clauses, each containing one or more OR terms.
        def matches(term):
            operator, value = re.fullmatch(r"FullyQualifiedName(!?~)(.+)", term.strip("()")).groups()
            contains = value.casefold() in name.casefold()
            return not contains if operator == "!~" else contains

        return all(any(matches(term) for term in clause.split("|")) for clause in expression.split("&"))

    def test_preflight_gates_build_specs_and_probes_and_cannot_be_hidden_by_the_aggregate(self):
        workflow = Path(__file__).with_name("workflows").joinpath("dotnet-build.yml").read_text()
        jobs = self.workflow_jobs()
        command = "python3 Verification/verify-runtime-compatibility.py --self-test"
        self.assertEqual(1, workflow.count(command))
        self.assertIn(command, jobs["preflight"])
        self.assertNotIn("setup-dotnet", jobs["preflight"])
        for name in ("dotnet-build", "specs", "reducer-mongo", "generated-name-matrix", "standalone-generated-application",
                     "live-custom-packages", "generated-react", "generated-react-blueprint-templates", "build"):
            self.assertRegex(jobs[name], r"needs: (?:\[[^\]\n]*)?preflight\b")
        aggregate = jobs["build"]
        self.assertIn("if: always()", aggregate)
        gated = (("PREFLIGHT_RESULT", "preflight"), ("BUILD_RESULT", "dotnet-build"), ("SPECS_RESULT", "specs"),
                 ("STANDALONE_RESULT", "standalone-generated-application"))
        for variable, job in gated:
            self.assertIn(variable + ": ${{ needs." + job + ".result }}", aggregate)
        check = re.search(r"run: (test .+)", aggregate)[1]
        for results in product(("success", "failure", "skipped", "cancelled"), repeat=len(gated)):
            with self.subTest(results=results):
                environment = dict(zip((variable for variable, _ in gated), results))
                self.assertEqual(all(result == "success" for result in results),
                                 subprocess.run(["sh", "-c", check], env=environment).returncode == 0)

    @staticmethod
    def write_trx(path, results, assembly="Cratis.Stage.Rendering.Cratis.dll"):
        """results: (class name, method, start second, end second, outcome); durations are always zero."""
        namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
        root = ET.Element("TestRun", xmlns=namespace)
        definitions = ET.SubElement(root, "TestDefinitions")
        executed = ET.SubElement(root, "Results")
        for index, (name, method, start, end, outcome) in enumerate(results):
            test = ET.SubElement(definitions, "UnitTest", id=str(index), name=name + "." + method)
            ET.SubElement(test, "TestMethod", className=name, name=method, codeBase="/runner/bin/" + assembly)
            ET.SubElement(executed, "UnitTestResult", testId=str(index), testName=name + "." + method,
                          outcome=outcome, duration="00:00:00.0000000",
                          startTime=f"2026-10-18T10:{start // 60:02d}:{start % 60:02d}+00:00",
                          endTime=f"2026-10-18T10:{end // 60:02d}:{end % 60:02d}+00:00")
        summary = ET.SubElement(root, "ResultSummary")
        ET.SubElement(summary, "Counters", executed=str(len(results)))
        path.parent.mkdir(parents=True, exist_ok=True)
        ET.ElementTree(root).write(path)

    def test_timings_weigh_prefixes_by_summed_class_wall_time_without_dedicated_fixtures(self):
        namespace = SHARDS["NAMESPACE"]
        planner = SHARDS["PLANNER"] + ".when_rendering_heavily."
        scaffold = namespace + ".for_CratisBackendApplicationScaffold."
        groups = {planner: 1, scaffold: 2}
        work = SHARDS["ROOT"] / ".ai-work"
        work.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(prefix="spec-shard-timing-tests-", dir=work) as directory, \
                contextlib.redirect_stdout(io.StringIO()):
            root = Path(directory)
            for run, scale in (("1", 1), ("2", 3)):
                for shard in range(SHARDS["RENDERING_SHARDS"]):
                    self.write_trx(root / run / f"shard-{shard}" / "specs.trx", [
                        # Shared Establish/Because: zero per-test duration, the class span is what it costs.
                        (planner[:-1], "should_a", 0, 10 * scale, "Passed"),
                        (planner[:-1], "should_b", 20 * scale, 30 * scale, "Passed"),
                        (planner[:-1] + ".with_nested_context", "should_c", 0, 5 * scale, "Passed"),
                        (scaffold + "when_building", "should_d", 0, 7 * scale, "Passed"),
                        (scaffold + SHARDS["STANDALONE_APPLICATION_FIXTURE"], "should_e", 0, 59, "Passed"),
                        (namespace + ".for_Unknown.when_new", "should_f", 0, 40, "Passed"),
                        (planner[:-1], "should_g", 0, 59, "NotExecuted"),
                    ] if shard == 0 else [(namespace + ".for_Unknown.when_new", "should_h", 0, 1, "Passed")])
            self.write_trx(root / "1" / "conformance" / "specs.trx",
                           [(planner[:-1], "should_x", 0, 59, "Passed")], assembly="Cratis.Stage.Conformance.Specs.dll")

            # Conformance runs planner namespaces too; only the Rendering assembly weighs Rendering.
            self.assertEqual(Counter({planner: 35, scaffold: 7}), SHARDS["run_timings"](root / "1", groups))
            (root / "1" / "conformance" / "specs.trx").unlink()  # An incomplete Conformance run is refused.
            timings = root / "timings.json"
            with patch.dict(SHARDS["refresh_timings"].__globals__,
                            {"TIMINGS": timings, "source_groups": lambda sharding: groups}):
                SHARDS["refresh_timings"](root)
            measured = json.loads(timings.read_text())
        self.assertEqual(["1", "2"], measured["runs"])
        self.assertEqual({planner: 70.0, scaffold: 14.0}, measured["prefix_seconds"])

    def test_timings_reject_failed_runs_and_missing_shards(self):
        name = SHARDS["PLANNER"] + ".when_rendering"
        work = SHARDS["ROOT"] / ".ai-work"
        work.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(prefix="spec-shard-timing-tests-", dir=work) as directory, \
                contextlib.redirect_stdout(io.StringIO()):
            root = Path(directory)
            for shard in range(SHARDS["RENDERING_SHARDS"] - 1):
                self.write_trx(root / "partial" / str(shard) / "specs.trx", [(name, "should_a", 0, 1, "Passed")])
            self.write_trx(root / "failed" / "0" / "specs.trx",
                           [(name, "should_a", 0, 1, "Passed"), (name, "should_b", 0, 1, "Failed")])
            for run in ("partial", "failed"):
                with self.subTest(run=run), self.assertRaises(SystemExit):
                    SHARDS["run_timings"](root / run, {name + ".": 1})

    def test_timings_attribute_idle_fixture_time_to_the_conformance_class_that_ends_it(self):
        conformance = SHARDS["CONFORMANCE_SHARDING"]
        first = SHARDS["THREE_WAY"] + ".when_first."
        second = SHARDS["PLANNER"] + ".when_second."
        third = "Cratis.Stage.Conformance.Specs.for_Small."
        groups = {first: 1, second: 1, third: 1}
        work = SHARDS["ROOT"] / ".ai-work"
        work.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(prefix="spec-shard-timing-tests-", dir=work) as directory, \
                contextlib.redirect_stdout(io.StringIO()):
            root = Path(directory)
            for run, offset in (("1", 0), ("2", 10)):
                for shard in range(conformance.shards):
                    # first runs 0-10; second's fixture idles 10-30, it runs 30-40 while third (20-35) overlaps.
                    results = [(first[:-1], "should_a", 0, 10, "Passed"),
                               (second[:-1], "should_b", 30 + offset, 40 + offset, "Passed"),
                               (third + "when_small", "should_c", 20, 35, "Passed")] if shard == 0 else \
                              [(third + "when_other", "should_d", 5, 6, "Passed")]
                    self.write_trx(root / run / f"conformance-{shard}" / "specs.trx", results,
                                   assembly=conformance.assembly)
            path = root / "1" / "conformance-0" / "specs.trx"
            # Sorted by start: first 10 s span; third starts at 20 after 10 s idle (15 + 10); second overlaps it.
            self.assertEqual({first[:-1]: 10.0, third + "when_small": 25.0, second[:-1]: 10.0},
                             SHARDS["wall_attributed"](ET.parse(path).getroot()))
            # Every TRX has its own timeline: the 1 s class in another shard adds its span, not a cross-file gap.
            self.assertEqual(Counter({first: 10.0, second: 10.0, third: 26.0 + 1.0}),
                             SHARDS["run_timings"](root / "1", groups, conformance))
            timings = root / "timings.json"
            timings.write_text(json.dumps({"measurement": "kept", "runs": ["r"], "prefix_seconds": {"Kept.": 1.0}}))
            with patch.dict(SHARDS["refresh_timings"].__globals__,
                            {"TIMINGS": timings, "source_groups": lambda sharding: groups}):
                SHARDS["refresh_timings"](root)
            data = json.loads(timings.read_text())
        # Rendering keeps its top-level section untouched when no Rendering TRX was provided.
        self.assertEqual({"Kept.": 1.0}, data["prefix_seconds"])
        self.assertEqual(["1", "2"], data["conformance"]["runs"])
        # Run 2 starts second at 40, after 5 idle seconds: (10 + 15) / 2.
        self.assertEqual({first: 10.0, second: 12.5, third: 27.0}, data["conformance"]["prefix_seconds"])
        with tempfile.TemporaryDirectory(prefix="spec-shard-timing-tests-", dir=work) as directory, \
                contextlib.redirect_stdout(io.StringIO()), self.assertRaises(SystemExit):
            # A pre-split, single-TRX Conformance run cannot masquerade as a complete sharded run.
            self.write_trx(Path(directory) / "conformance" / "specs.trx", [(first[:-1], "should_a", 0, 1, "Passed")],
                           assembly=conformance.assembly)
            SHARDS["run_timings"](Path(directory), groups, conformance)

    def test_measured_timings_cover_no_dedicated_fixture(self):
        data = json.loads(SHARDS["TIMINGS"].read_text())
        for sharding in SHARDS["SHARDED"]:
            measured = SHARDS["timings_section"](data, sharding)["prefix_seconds"]
            self.assertTrue(measured, sharding.name)
            self.assertFalse([prefix for prefix in measured if SHARDS["dedicated"](prefix)], sharding.name)

    def test_nested_namespaces_are_owned_by_one_prefix(self):
        root = "Cratis.Stage.Rendering.Cratis.for_Planner."
        nested = root + "when_rendering."
        groups = SHARDS["merge_nested_groups"](Counter({root: 2, nested: 3, nested.upper(): 1}))
        self.assertEqual(Counter({root: 6}), groups)

    def test_build_generated_spec_sources_do_not_change_balancing(self):
        work = SHARDS["ROOT"] / ".ai-work"
        work.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(prefix="spec-shard-source-tests-", dir=work) as directory:
            root = Path(directory)
            sources = root / "Source/Rendering.Cratis"
            sources.mkdir(parents=True)
            text = "namespace Cratis.Stage.Rendering.Cratis.for_Test;\npublic class when_testing { [Fact] void should_pass() {} }"
            (sources / "when_testing.cs").write_text(text)
            for output in ("bin", "obj"):
                generated = sources / output
                generated.mkdir()
                (generated / "generated.cs").write_text(text)
            with patch.dict(SHARDS["source_groups"].__globals__, {"ROOT": root}):
                self.assertEqual(Counter({"Cratis.Stage.Rendering.Cratis.for_Test.": 1}), SHARDS["source_groups"]())

    def test_solution_guard_accepts_exact_project_coverage(self):
        with patch("subprocess.run") as run, contextlib.redirect_stdout(io.StringIO()):
            run.side_effect = project_test_status
            SHARDS["guard_solution"](SHARDS["matrix"]())
            self.assertEqual(12, run.call_count)
            for call in run.call_args_list:
                self.assertIn("-property:Configuration=Debug", call.args[0])
                self.assertTrue(call.kwargs["check"])

    def test_solution_guard_rejects_a_new_unsharded_test_project(self):
        with patch("subprocess.run") as run, self.assertRaisesRegex(
                SystemExit, r"missing=\['Source/BodyWorker/BodyWorker.csproj'\]"):
            run.return_value.stdout = "true\n"
            SHARDS["guard_solution"](SHARDS["matrix"]())

    def test_solution_guard_requires_exactly_three_conformance_shards(self):
        original = SHARDS["matrix"]()
        conformance = [shard for shard in original["include"] if SHARDS["CONFORMANCE"] in shard["projects"]]
        others = [shard for shard in original["include"] if SHARDS["CONFORMANCE"] not in shard["projects"]]
        for shards in (conformance[:2], conformance + conformance[:1]):
            with self.subTest(count=len(shards)), patch("subprocess.run") as run, self.assertRaises(SystemExit):
                run.side_effect = project_test_status
                SHARDS["guard_solution"]({"include": others + shards})

    def test_solution_guard_rejects_unsharded_or_duplicate_projects(self):
        original = SHARDS["matrix"]()
        missing = json.loads(json.dumps(original))
        missing["include"].pop(0)
        duplicate = json.loads(json.dumps(original))
        duplicate["include"].append(duplicate["include"][0])
        for shards in (missing, duplicate):
            with patch("subprocess.run") as run, self.assertRaises(SystemExit):
                run.side_effect = project_test_status
                SHARDS["guard_solution"](shards)

    def test_shard_runs_every_project_and_reports_all_failures(self):
        shard = {"name": "multi", "filter": "F", "projects": ["A/A.csproj", "B/B.csproj", "C/C.csproj"]}
        exits = {"A": 1, "B": 0, "C": 3}

        def fake_run(command, **kwargs):
            return type("Completed", (), {"returncode": exits[Path(command[2]).stem]})()

        work = SHARDS["ROOT"] / ".ai-work"
        work.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(prefix="spec-shard-tests-", dir=work) as directory, \
                patch.dict(SHARDS["run_shard"].__globals__, {"ROOT": Path(directory)}), \
                patch("subprocess.run", side_effect=fake_run) as run, \
                patch.dict(SHARDS["run_shard"].__globals__, {"require_executed": lambda path: None}):
            with self.assertRaises(SystemExit) as raised:
                SHARDS["run_shard"](shard)
        self.assertEqual(3, run.call_count)
        self.assertIn("A: dotnet test exited 1", str(raised.exception))
        self.assertIn("C: dotnet test exited 3", str(raised.exception))
        self.assertNotIn("B:", str(raised.exception))

    def test_trx_guard_rejects_empty_or_skipped_runs_and_accepts_execution(self):
        # All fixture files are task-owned, tiny, and cleaned by TemporaryDirectory.
        work = SHARDS["ROOT"] / ".ai-work"
        work.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(prefix="spec-shard-tests-", dir=work) as directory:
            path = Path(directory) / "specs.trx"
            namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
            for executed, outcome in ((0, "NotExecuted"), (0, "Passed"), (1, "NotExecuted"), (1, "Passed")):
                root = ET.Element("TestRun", xmlns=namespace)
                results = ET.SubElement(root, "Results")
                ET.SubElement(results, "UnitTestResult", outcome=outcome)
                summary = ET.SubElement(root, "ResultSummary")
                ET.SubElement(summary, "Counters", executed=str(executed))
                ET.ElementTree(root).write(path)
                if executed and outcome == "Passed":
                    with contextlib.redirect_stdout(io.StringIO()):
                        SHARDS["require_executed"](path)
                else:
                    with self.assertRaises(SystemExit):
                        SHARDS["require_executed"](path)


if __name__ == "__main__":
    unittest.main()
