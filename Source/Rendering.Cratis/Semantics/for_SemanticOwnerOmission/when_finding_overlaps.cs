// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission;

public class when_finding_overlaps : given.an_omission_context
{
    PlannedArtifact _omitted = null!;
    PlannedArtifact _kept = null!;
    IReadOnlyList<PlannedArtifact> _overlaps = null!;
    IReadOnlyList<PlannedArtifact> _none = null!;

    void Because()
    {
        var @event = _registerProject.Events.Single().Id;
        _omitted = PlannedArtifact.CreateText("Omitted.cs", "class Omitted {}", [@event]);
        _kept = PlannedArtifact.CreateText("Kept.cs", "class Kept {}", [_projectId.Id]);
        _overlaps = SemanticOwnerOmission.Overlaps([_kept, _omitted], [@event]);
        _none = SemanticOwnerOmission.Overlaps([_kept, _omitted], []);
    }

    [Fact] void should_return_only_the_artifact_realizing_an_omitted_owner() => _overlaps.ShouldContainOnly(_omitted);
    [Fact] void should_return_nothing_for_an_empty_closure() => _none.ShouldBeEmpty();
}
