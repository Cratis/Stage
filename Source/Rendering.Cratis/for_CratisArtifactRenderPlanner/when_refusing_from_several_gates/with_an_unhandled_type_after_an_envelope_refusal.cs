// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_an_unhandled_type_after_an_envelope_refusal : given.a_collecting_render_request
{
    void Establish()
    {
        var command = _registerProject.Commands.Single();
        _request = WithGuard(WithChange(_registerProject with
        {
            Commands = [command with
            {
                Properties = [.. command.Properties.Select(property => property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } })]
            }]
        })) with { ImplementationRequirements = default };
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_preserve_the_collected_refusal() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
    [Fact] void should_report_the_thrown_semantic_variant() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("999", StringComparison.Ordinal));
    [Fact] void should_continue_to_independent_admission() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-SCENE-ACTION-001");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
