// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_planning_the_standalone_long_match : Specification
{
    [Fact]
    void should_render_the_long_concept_and_both_server_side_specifications()
    {
        var model = when_verifying_a_standalone_generated_application.CreateLongMatchModel();
        var slice = model.Application.Modules.Single(module => module.Name == "LongMatch").Features.Single().Slices.Single();
        var valueProperty = slice.Commands.Single().Properties.Single(property => property.Name == "value");
        valueProperty.Type.Target.ShouldEqual(model.Application.Concepts.Single(concept => concept.Name == "LongMatchValue").Id);
        var specifications = slice.Specifications;
        ((SemanticTextValue)specifications.Single(specification => specification.Name == "AcceptingALongMatch").When!.Values.Single(value => value.TargetProperty == valueProperty.Id).Value).Value.ShouldEqual(new string('a', 2996) + "b");
        ((SemanticTextValue)specifications.Single(specification => specification.Name == "RejectingALongMatch").When!.Values.Single(value => value.TargetProperty == valueProperty.Id).Value).Value.ShouldEqual(new string('a', 2995) + "b");
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        plan.Success.ShouldBeTrue();
        Text(plan, "Common/LongMatchValue.cs").ShouldContain("PortableMatcher.Compile(\"^(a{2996}b)+$\")");
        var accepted = Text(plan, "LongMatch/Checking/Check/when_accepting_a_long_match.cs");
        accepted.ShouldContain("await _scenario.Execute(");
        accepted.ShouldContain("should_return_the_expected_response");
        var rejected = Text(plan, "LongMatch/Checking/Check/when_rejecting_a_long_match.cs");
        rejected.ShouldContain("await _scenario.Execute(");
        rejected.ShouldContain("_result.ValidationResults.First().Message.ShouldEqual(\"Long match rejected\")");
    }

    static string Text(ArtifactRenderPlan plan, string suffix) => Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(suffix, StringComparison.Ordinal)).Bytes.AsSpan());
}
