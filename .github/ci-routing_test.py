"""Unit tests for ci-routing.py: python3 .github/ci-routing_test.py"""
import importlib.util
import os
import unittest
from pathlib import Path
from unittest import mock

spec = importlib.util.spec_from_file_location("ci_routing", Path(__file__).with_name("ci-routing.py"))
routing = importlib.util.module_from_spec(spec)
spec.loader.exec_module(routing)

ENV = {"GITHUB_REPOSITORY": "Cratis/Stage", "GITHUB_SHA": "abc", "GITHUB_RUN_ID": "1"}
RUNS = '{"workflow_runs": []}'


def fake_gh(responses, calls):
    def run(*arguments):
        calls.append(arguments)
        key = tuple(arguments[:2])
        return responses.get(key, "")
    return run


class ReportNightlyTests(unittest.TestCase):
    def report(self, responses, dry_run="false"):
        calls = []
        env = {**ENV, "NIGHTLY_DRY_RUN": dry_run}
        with mock.patch.dict(os.environ, env), mock.patch.object(routing, "gh", fake_gh(responses, calls)):
            routing.report_nightly()
        return [call[:2] for call in calls]

    def test_missing_label_prints_empty_output_and_is_created(self):
        # gh prints nothing for an empty `label list --json`; this crashed json.loads before.
        responses = {("api", "repos/Cratis/Stage/actions/workflows/dotnet-build.yml/runs?event=schedule&status=success&per_page=1"): RUNS,
                     ("issue", "list"): "[]", ("label", "list"): ""}
        self.assertEqual(self.report(responses)[-2:], [("label", "create"), ("issue", "create")])

    def test_existing_issue_gets_a_comment(self):
        responses = {("api", "repos/Cratis/Stage/actions/workflows/dotnet-build.yml/runs?event=schedule&status=success&per_page=1"): RUNS,
                     ("issue", "list"): '[{"number": 7}]'}
        self.assertEqual(self.report(responses)[-1], ("issue", "comment"))

    def test_dry_run_writes_nothing(self):
        responses = {("api", "repos/Cratis/Stage/actions/workflows/dotnet-build.yml/runs?event=schedule&status=success&per_page=1"): RUNS,
                     ("issue", "list"): ""}
        calls = self.report(responses, "true")
        self.assertNotIn(("issue", "create"), calls)
        self.assertNotIn(("label", "create"), calls)


if __name__ == "__main__":
    unittest.main()
