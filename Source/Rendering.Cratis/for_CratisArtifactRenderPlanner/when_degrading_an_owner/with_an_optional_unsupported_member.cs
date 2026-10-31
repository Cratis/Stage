// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_an_optional_unsupported_member : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var owner = _registerProject.Events.Single();
        Replace(owner, "Properties", owner.Properties.Select(property => property.Type.Kind == (global::Cratis.Screenplay.Semantics.SemanticTypeReferenceKind)999
            ? property with { Type = property.Type with { IsOptional = true } } : property).ToImmutableArray());
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_omit_the_whole_event_not_only_the_optional_member() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_unsupportedEvent)).ShouldBeFalse();
    [Fact] void should_keep_the_independent_command() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_independentCommand)).ShouldBeTrue();
    [Fact] void should_degrade_the_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
}
