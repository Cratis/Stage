// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_a_specification_asserts_uncontrolled_metadata : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = event_context_projection_model.Plan(a_native_context_model.Source + "\n" + """
              specification MetadataIsNotControlled
                given ContextRecorded
                  for "context-source"
                  name = "payload"
                then readmodel FromTotal
                  id = "context-source"
                  occurred = "2027-03-04T10:00:00.123Z"
            slice StateChange Recording
              command Record
                id String identifier
                name String
                produces ContextRecorded
                  for id
                  name = name
        """);

    [Fact] void should_not_render_the_unbound_assertion() => event_context_projection_model.Source(_plan).ShouldNotContain("[Fact]");
    [Fact] void should_explain_the_unbound_metadata() => _plan.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("explicitly bound event metadata", StringComparison.Ordinal)).ShouldBeTrue();
}
