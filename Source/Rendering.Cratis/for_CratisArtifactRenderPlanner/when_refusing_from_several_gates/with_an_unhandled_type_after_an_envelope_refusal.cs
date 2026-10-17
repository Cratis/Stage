// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_an_unhandled_type_after_an_envelope_refusal : given.a_collecting_render_request
{
    void Establish()
    {
        // Fault injection: simulate a future compiler's type variant after compiling a valid fixture.
        // Use an event property: command admission calls TypeExists for produced event properties.
        var @event = _registerProject.Events.Single();
        var properties = @event.Properties.Select(property => property.Name == "name"
            ? property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } }
            : property).ToImmutableArray();
        typeof(SemanticEventContract).GetField("<Properties>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(@event, properties);
        _request = WithGuard(_request) with { ImplementationRequirements = default };
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_preserve_the_collected_refusal() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020");
    [Fact] void should_report_the_thrown_semantic_variant() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("999", StringComparison.Ordinal));
    [Fact] void should_continue_to_independent_admission() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-SCENE-ACTION-001");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
