// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_a_concrete_unresolved_property_target : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;
    SemanticDependencyIndex _index = null!;

    void Establish()
    {
        var owner = _registerProject.Events.Single();
        Replace(owner, "Properties", owner.Properties.OrderByDescending(property => property.Type.Kind == (SemanticTypeReferenceKind)999).Select(property => property.Type.Kind != (SemanticTypeReferenceKind)999
            ? property with { Type = SemanticTypeReference.ForConcept(SemanticId.Create(SemanticKind.Concept, "Missing")) } : property).ToImmutableArray());
    }

    void Because()
    {
        _index = new(new SemanticApplicationContext(_request, _options));
        _plan = _planner.Plan(_request);
    }

    [Fact] void should_build_real_unresolved_target_provenance() => _index.Incompleteness.Any(entry => entry.Owner == _unsupportedEvent && entry.Kind == SemanticDependencyIncompletenessKind.UnresolvedIdentity && entry.PropertyTypeOf == _unsupportedEvent).ShouldBeTrue();
    [Fact] void should_locate_the_gap_in_the_event_property_type() => _index.Incompleteness.Single(entry => entry.Owner == _unsupportedEvent && entry.Kind == SemanticDependencyIncompletenessKind.UnresolvedIdentity).Member.ShouldEqual("SemanticTypeReference.Target");
    [Fact] void should_admit_the_root_local_gap() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_render_the_independent_command() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_independentCommand)).ShouldBeTrue();
    [Fact] void should_not_emit_the_root() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_unsupportedEvent)).ShouldBeFalse();
}
