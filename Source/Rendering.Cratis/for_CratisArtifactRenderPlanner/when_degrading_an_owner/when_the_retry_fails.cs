// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class when_the_retry_fails : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _withoutRoot = null!;

    void Establish() => ChangeApplication(application => application with
    {
        Modules = [.. application.Modules.Select(module => module.Name == "Billing" ? module with { Name = "Customizations" } : module)]
    });

    void Because()
    {
        _plan = _planner.Plan(_request);
        var owner = _registerProject.Events.Single();
        var unsupported = owner.Properties;
        Replace(owner, "Properties", unsupported.Select(property => property.Type.Kind == (SemanticTypeReferenceKind)999
            ? property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) } : property).ToImmutableArray());
        _withoutRoot = _planner.Plan(_request);
        Replace(owner, "Properties", unsupported);
    }

    [Fact] void should_witness_the_late_artifact_path_error_without_the_root() => _withoutRoot.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-CRATIS-005").ShouldBeTrue();
    [Fact] void should_refuse_instead_of_publishing_a_partial_retry() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_restore_the_original_error() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == _unsupportedEvent).ShouldBeTrue();
    [Fact] void should_not_replace_the_original_error_with_the_retry_error() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-CRATIS-005").ShouldBeFalse();
    [Fact] void should_not_publish_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_publish_removals() => _plan.RequiredRemovals.ShouldBeEmpty();
    [Fact] void should_not_publish_degradations() => _plan.Degradations.ShouldBeEmpty();
}
