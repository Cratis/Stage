// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

public class when_running_a_model_with_a_refused_specification : Specification
{
    string _directory = null!;
    string _path = null!;
    LoadedSemanticModel _loaded = null!;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-refused-spec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "Projects.play");
        await File.WriteAllTextAsync(_path, """
            module Projects
              feature Registration
                slice StateChange Register
                  command Register
                    id String identifier
                    produces Registered
                      for id
                  event Registered
                  specification Healthy
                    when Register
                      id = "one"
                    then Registered
                      for "one"
                  specification Broken
                    when Register
                      id = "two"
                    then NoSuchEvent
                      for "two"
            """);
        _loaded = await SemanticModelLoader.LoadFromPathAsync(_path, null, null, allowRefusedSpecifications: true);
    }

    void Destroy() => Directory.Delete(_directory, true);

    [Theory]
    [InlineData(false, "healthy", 1)]
    [InlineData(true, "healthy", 0)]
    [InlineData(true, "broken", 1)]
    [InlineData(true, "all", 1)]
    public async Task should_only_run_a_healthy_selection_when_the_host_opts_in(bool allowRefused, string selection, int expected)
    {
        var output = Path.Combine(_directory, $"{allowRefused}-{selection}.json");
        string[] arguments = ["--engine", "semantic", "--model", _path, "--output", output];
        if (allowRefused) arguments = [.. arguments, "--allow-refused-specifications", "true"];
        if (selection == "healthy") arguments = [.. arguments, "--scope", _loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().Id.ToString()];
        if (selection == "broken") arguments = [.. arguments, "--scope", _loaded.RefusedSpecifications.Single().Id.ToString()];
        using var error = new StringWriter();
        var exit = await Program.Run(arguments, TextWriter.Null, error);
        exit.ShouldEqual(expected);
        if (expected == 0)
        {
            SemanticSpecificationRunReportFile.Read(await File.ReadAllTextAsync(output))!.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        }
        else
        {
            error.ToString().ShouldContain(_loaded.RefusedSpecifications.Single().Diagnostics[0].Message);
            File.Exists(output).ShouldBeFalse();
        }
    }
}
