// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_sources_with_information_and_warnings : given.a_source_plan
{
    void Establish() => File.WriteAllText(Path.Combine(_root, "metadata.play"), """
        concept Status : Enum
          draft
          validate
          sent
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                description "Register a project"
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
              event ProjectRegistered
                description "A project was registered"
        """);

    async Task Because() => _result = await From("metadata.play");

    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_preserve_information_diagnostics() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0270" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Information).ShouldEqual(2);
    [Fact] void should_preserve_the_warning() => _result.Diagnostics.Count(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldEqual(1);
    [Fact] void should_preserve_source_locations() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0270").All(diagnostic => diagnostic.Source!.StartsWith("metadata.play(", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_carry_information_into_the_underlying_plan() => _result.Plan!.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0270" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Information).ShouldEqual(2);
    [Fact] void should_carry_the_warning_into_the_underlying_plan() => _result.Plan!.Diagnostics.Count(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldEqual(1);
}
