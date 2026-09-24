// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_running_semantic_engine : Specification
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"stage semantic runner {Guid.NewGuid():N}");
    SemanticSpecificationRunReport _report = null!;
    int _exitCode;

    void Establish()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "Projects.play"),
            """
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
                    when RegisterProject
                      name = ""
                    then error "Project name is required"
            """);
    }

    async Task Because()
    {
        var result = Path.Combine(_folder, "semantic.json");
        _exitCode = await Program.Run(["--engine", "semantic", "--model", _folder, "--output", result], TextWriter.Null, TextWriter.Null);
        if (File.Exists(result)) _report = await SemanticSpecificationRunReportFile.ReadFromFile(result);
    }

    [Fact] void should_write_a_versioned_report() => _report.SchemaVersion.ShouldEqual("stage-spec-run/1");
    [Fact] void should_complete_the_semantic_run() => _exitCode.ShouldEqual(0);
    [Fact] void should_preserve_semantic_identity() => _report.Results.Single().SpecificationId.StartsWith("sem1:", StringComparison.Ordinal).ShouldBeTrue();

    void Destroy() => Directory.Delete(_folder, true);
}
#endif
