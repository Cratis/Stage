// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_running_semantic_case_tables : Specification
{
    string _folder = null!;
    SemanticSpecificationRunReport _report = null!;
    int _exitCode;

    async Task Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), "cratis-stage-specs", "case-table-runs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        await File.WriteAllTextAsync(Path.Combine(_folder, "Projects.play"), """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    name String
                    validate
                      name not empty message "Project name is required"
                    produces ProjectRegistered
                      name = name
                  event ProjectRegistered
                    name String
                  specification RejectingEmptyName
                    parameter reason String
                    case Passing reason = "Project name is required"
                    case Failing reason = "Wrong reason"
                    when RegisterProject
                      name = ""
                    then error case.reason
            """);
    }

    async Task Because()
    {
        var result = Path.Combine(_folder, "semantic.json");
        _exitCode = await Program.Run(["--engine", "semantic", "--model", _folder, "--output", result], TextWriter.Null, TextWriter.Null);
        _report = await SemanticSpecificationRunReportFile.ReadFromFile(result);
    }

    [Fact] void should_complete_the_run_even_with_a_failing_case() => _exitCode.ShouldEqual(0);
    [Fact] void should_keep_the_report_schema() => _report.SchemaVersion.ShouldEqual("stage-spec-run/1");
    [Fact] void should_report_each_case() => _report.Results.Select(result => result.Name).ShouldContainOnly("RejectingEmptyName_Passing", "RejectingEmptyName_Failing");
    [Fact] void should_keep_the_passing_case_free_of_failures() => _report.Results.Single(result => result.Outcome == SemanticSpecificationOutcome.Passed).Failures.ShouldBeEmpty();
    [Fact] void should_write_the_case_name_and_values_to_the_existing_failure_field() => _report.Results.Single(result => result.Outcome == SemanticSpecificationOutcome.Failed).Failures.Single().ShouldContain("Case 'Failing' of 'RejectingEmptyName':");
    [Fact] void should_preserve_the_case_fixture_in_the_report() => _report.Results.Single(result => result.Outcome == SemanticSpecificationOutcome.Failed).Failures.Single().ShouldContain("then error: message = \"Wrong reason\" (case)");

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
#endif
