// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_public_translation_contracts_are_only_expected : given.a_translation_application
{
    SemanticId _publicContract;

    void Establish()
    {
        var canonical = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.ToArray());
        var publication = canonical.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "PublishOrderShipped");
        _publicContract = publication.Events.Single().Id;
        var specification = publication.Specifications.Single() with
        {
            Id = SemanticId.Create(SemanticKind.Specification, "outside-translation"),
            Name = "ExpectingAPublicContract"
        };
        _model = Transform(_model, slice => slice.Name == "PackOrder" ? slice with { Specifications = [specification] } : slice);
    }

    void Because() => _plan = Plan(_model, ArtifactRefusalMode.RenderAround);

    [Fact] void should_not_refuse_the_public_contract_for_being_expected_elsewhere() =>
        _plan.Refusals.Any(refusal => refusal.Artifact == _publicContract && refusal.Codes.Contains("STAGE-ESM-032")).ShouldBeFalse();

    [Fact] void should_keep_the_outbox_publisher() =>
        _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("OrderShippedPublisher.cs", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact] void should_keep_unrelated_specification_limits_explicit() =>
        _plan.Refusals.Any(refusal => refusal.Kind == "specification").ShouldBeTrue();
}
#endif
