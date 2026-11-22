// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_carrying_semantic_exclusions;

public class and_using_application_fallback : given.a_request_with_exclusions
{
    void Establish() => _request = _request with
    {
        Exclusions = [_slice with { Kind = SemanticExclusionKind.Import, Address = "Missing.Import", ParentId = null },
            _slice with { Kind = SemanticExclusionKind.Policy, Address = "MissingPolicy", ParentId = default(SemanticId) },
            _slice with { Kind = SemanticExclusionKind.Trigger, Address = "MissingTrigger", ParentId = null }]
    };

    void Because() => _result = Create();

    [Fact] void should_retain_every_application_level_kind() => _result.Exclusions.Length.ShouldEqual(3);
    [Fact] void should_own_missing_parent_evidence_on_the_application() => _result.Diagnostics.All(diagnostic => diagnostic.Artifact == _request.Model.Application.Id).ShouldBeTrue();
    [Fact] void should_allow_digesting_unset_parent_metadata() => _result.Digest.Length.ShouldEqual(64);
}
