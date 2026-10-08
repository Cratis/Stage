// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_selecting_repeated_semantic_scopes : Specification
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"stage-scopes-{Guid.NewGuid():N}");
    const string First = "sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string Second = "sem1:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    int _exit;
    SemanticSpecificationRunReport _report = null!;

    void Establish()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "Projects.play"), "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered\n        name String\n");
    }

    async Task Because()
    {
        var path = Path.Combine(_folder, "results.json");
        _exit = await Program.Run(["--engine", "semantic", "--model", _folder, "--output", path, "--scope", First, "--scope", Second], TextWriter.Null, TextWriter.Null);
        _report = (await SemanticSpecificationRunReportFile.ReadFromFile(path))!;
    }

    [Fact] void should_complete_the_run() => _exit.ShouldEqual(0);
    [Fact] void should_preserve_every_scope() => _report.Results.Select(record => record.SpecificationId).ShouldContainOnly(First, Second);
    [Fact] void should_report_unknown_scopes_as_unsupported() => _report.Results.All(record => record.Outcome == SemanticSpecificationOutcome.Unsupported).ShouldBeTrue();

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
