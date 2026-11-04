// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

// The personal classification takes precedence over the secret one and renders only the PII attribute, with no NotAudited,
// so a concept that is both personal and secret needs nothing the contracts project cannot reference.
public class when_a_public_event_uses_a_personal_secret_concept : a_translation_application
{
    string _concept = string.Empty;

    void Because()
    {
        var complianceModel = a_concept_compliance_model.Compile(ConceptComplianceCorpus.V13.SourceForms[0]);
        var compliance = complianceModel.Application.Concepts.Single(concept => concept.Name == "PersonalSecret").Compliance;
        var published = _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "PublishOrderShipped").Events.Single(@event => @event.Visibility == SemanticEventVisibility.Public && @event.Origin is null);
        var target = published.Properties.First(property => property.Type.Kind == SemanticTypeReferenceKind.Concept).Type.Target;
        _model = ExecutableSemanticModel.Create(complianceModel.LanguageVersion, complianceModel.SemanticVersion, _model.Application with
        {
            Concepts = [.. _model.Application.Concepts.Select(concept => concept.Id == target ? concept with { Compliance = compliance } : concept)]
        });
        Render();
        _concept = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.StartsWith("Contracts.Events.Contracts/", StringComparison.Ordinal) && artifact.RelativePath.EndsWith($"/{_model.Application.Concepts.Single(candidate => candidate.Id == target).Name}.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_keep_the_concept_in_the_contracts_project() => _concept.ShouldContain("[global::Cratis.Chronicle.Compliance.GDPR.PIIAttribute]");
    [Fact] void should_not_render_the_arc_metadata() => _concept.ShouldNotContain("NotAudited");
    [Fact] void should_compile_without_errors() => RenderedOutput.Errors(_sources).ShouldBeEmpty();
}
#endif
