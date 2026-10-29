// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_reordered_owners_without_omission : given.an_unsupported_event_request
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _reordered = null!;

    void Establish()
    {
        var owner = _registerProject.Events.Single();
        var originalType = _registerProject.Commands.Single().Properties.Single(property => property.Name == "name").Type;
        typeof(SemanticEventContract).GetField("<Properties>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(owner, owner.Properties.Select(property => property.Name == "name" ? property with { Type = originalType } : property).ToImmutableArray());
    }

    void Because()
    {
        _first = _planner.Plan(_request);
        var reordered = _model with { };
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(reordered, _model.Application with { Modules = [.. _model.Application.Modules.Reverse()] });
        _reordered = _planner.Plan(_request with { Model = reordered });
    }

    [Fact] void should_render_the_control_in_both_orders() => Assert.True(_first.Outcome == ArtifactRenderOutcome.Complete && _reordered.Outcome == ArtifactRenderOutcome.Complete,
        $"First: {_first.Outcome}; reordered: {_reordered.Outcome}; diagnostics: {string.Join(" | ", _first.Diagnostics.Concat(_reordered.Diagnostics).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"))}");
    [Fact] void should_keep_the_control_artifact_paths_and_hashes() => _reordered.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256))
        .ShouldContainOnly(_first.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)));
    [Fact] void should_keep_the_control_digest() => _reordered.Digest.ShouldEqual(_first.Digest);
}
