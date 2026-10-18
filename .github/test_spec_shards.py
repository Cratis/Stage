#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fast checks for lossless shard routing and fail-closed CI guards."""

from collections import Counter
import contextlib
import io
import json
from pathlib import Path
import re
import runpy
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

SHARDS = runpy.run_path(str(Path(__file__).with_name("spec-shards.py")))


class SpecShardsTests(unittest.TestCase):
    def test_every_known_prefix_and_new_namespace_has_exactly_one_shard(self):
        partitions = SHARDS["rendering_partitions"]()
        positive = partitions[:-1]
        names = [prefix + "should_pass" for prefix in SHARDS["source_groups"]()]
        names.append(SHARDS["NAMESPACE"] + ".for_A_New_Renderer.when_added.should_pass")
        for name in names:
            matches = [any(prefix.lower() in name.lower() for prefix in partition)
                       for partition in positive]
            self.assertEqual(1, sum(matches + [not any(matches)]), name)

    def test_final_filter_negates_every_positive_prefix(self):
        partitions = SHARDS["rendering_partitions"]()
        shards = SHARDS["matrix"]()["include"]
        expected = SHARDS["EXCLUSIONS"] + "&" + "&".join(
            "FullyQualifiedName!~" + prefix
            for partition in partitions[:-1] for prefix in partition)
        self.assertEqual(expected, shards[-1]["filter"])
        self.assertTrue(all(shard["filter"].startswith(SHARDS["EXCLUSIONS"]) for shard in shards))

    def test_blueprint_templates_run_exactly_once_with_or_without_the_dedicated_lane(self):
        workflow = Path(__file__).with_name("workflows").joinpath("dotnet-build.yml").read_text()
        lane = workflow.split("  generated-react-blueprint-templates:\n", 1)[1]
        dedicated_filter = re.search(r'--filter "([^"]+)"', lane)[1]
        name = SHARDS["PLANNER"] + "." + SHARDS["BLUEPRINT_TEMPLATES_FIXTURE"] + ".should_pass"
        self.assertIn("spec-shards.py guard --blueprint-templates-lane", workflow)
        for dedicated_lane in (False, True):
            with self.subTest(dedicated_lane=dedicated_lane):
                shards = SHARDS["matrix"](blueprint_templates_lane=dedicated_lane)["include"]
                selected = sum(self.matches_filter(name, shard["filter"])
                               for shard in shards if SHARDS["RENDERING"] in shard["projects"])
                self.assertEqual(0 if dedicated_lane else 1, selected)
                self.assertEqual(1, selected + int(dedicated_lane and self.matches_filter(name, dedicated_filter)))

    @staticmethod
    def matches_filter(name, expression):
        # Shard filters are an AND of clauses, each containing one or more OR terms.
        def matches(term):
            operator, value = re.fullmatch(r"FullyQualifiedName(!?~)(.+)", term.strip("()")).groups()
            contains = value.casefold() in name.casefold()
            return not contains if operator == "!~" else contains

        return all(any(matches(term) for term in clause.split("|")) for clause in expression.split("&"))

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
            run.return_value.stdout = "true\n"
            SHARDS["guard_solution"](SHARDS["matrix"]())
            self.assertEqual(8, run.call_count)
            for call in run.call_args_list:
                self.assertIn("-property:Configuration=Debug", call.args[0])
                self.assertTrue(call.kwargs["check"])

    def test_solution_guard_rejects_unsharded_or_duplicate_projects(self):
        original = SHARDS["matrix"]()
        missing = json.loads(json.dumps(original))
        missing["include"].pop(0)
        duplicate = json.loads(json.dumps(original))
        duplicate["include"].append(duplicate["include"][0])
        for shards in (missing, duplicate):
            with patch("subprocess.run") as run, self.assertRaises(SystemExit):
                run.return_value.stdout = "true\n"
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
