// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_multiple_unordered_events : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(with_unordered_events.Source));

    [Fact] void should_admit_reversed_event_expectations() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_a_consuming_multiset_matcher() => System.Text.Encoding.UTF8.GetString(
        _plan.Artifacts.Single(_ => _.RelativePath.Contains("/when_issuing", StringComparison.Ordinal)).Bytes.AsSpan())
        .ShouldContain("remaining.RemoveAt(match1)");
}
