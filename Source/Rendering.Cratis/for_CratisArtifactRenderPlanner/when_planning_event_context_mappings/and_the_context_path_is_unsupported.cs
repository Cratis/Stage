// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_the_context_path_is_unsupported : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _strict = null!;
    ArtifactRenderPlan _required = null!;
    ArtifactRenderPlan _filtered = null!;
    ArtifactRenderPlan _decision = null!;

    void Because()
    {
        _plan = event_context_projection_model.Plan(Unsupported(event_context_projection_model.Plain));
        _strict = event_context_projection_model.Plan(Unsupported(event_context_projection_model.Plain), strict: true);
        _required = event_context_projection_model.Plan(Unsupported(event_context_projection_model.RequiredNestedLeaf));
        _filtered = event_context_projection_model.Plan(event_context_projection_model.FilteredByContext
            .Replace("correlation Uuid", "correlation String", StringComparison.Ordinal)
            .Replace("$eventContext.correlationId", "$eventContext.causedBy.name", StringComparison.Ordinal));
        _decision = event_context_projection_model.Plan(Unsupported(event_context_projection_model.ReadByACommand));
    }

    [Fact] void should_degrade_only_the_unsupported_path() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct).ShouldBeTrue();
    [Fact] void should_keep_supported_correlation() => event_context_projection_model.Source(_plan).ShouldContain("ToEventContextProperty(c => c.CorrelationId)");
    [Fact] void should_refuse_in_strict_mode() => _strict.Success.ShouldBeFalse();
    [Fact] void should_not_emit_an_arbitrary_context_member() => event_context_projection_model.Source(_plan).ShouldNotContain("c.CausedBy.Name");
    [Fact] void should_protect_required_nested_leaves() => _required.Success.ShouldBeFalse();
    [Fact] void should_protect_query_filters() => _filtered.Success.ShouldBeFalse();
    [Fact] void should_protect_command_decisions() => _decision.Success.ShouldBeFalse();

    static string Unsupported(string source) => source
        .Replace("DateTime", "String", StringComparison.Ordinal)
        .Replace("$eventContext.occurred", "$eventContext.causedBy.name", StringComparison.Ordinal);
}
