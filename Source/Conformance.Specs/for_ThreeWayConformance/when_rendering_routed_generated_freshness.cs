// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_rendering_routed_generated_freshness : Specification
{
    string _command = string.Empty;
    string _specification = string.Empty;

    void Because()
    {
        var model = routed_generated_value_cases.Compile();
        model.Application.Concepts.Single(concept => concept.Name == "Receipt").Generation.ShouldNotBeNull();
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        _command = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Uuid.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        _specification = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/when_uuid_cross_stream_reuse.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_return_atomic_scopes_for_the_generated_destination() => _command.ShouldContain("EventSequences.EventsWithConcurrencyScopes Handle(");
    [Fact] void should_use_the_generated_identifier_for_the_freshness_scope() => _command.ShouldContain("EventSequenceNumber.BeforeFirst, EventSourceId: source");
    [Fact] void should_seed_the_other_stream() => _specification.ShouldContain("another-stream");
}
#endif
