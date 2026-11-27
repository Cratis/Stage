// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

// Personal secrets retain PII protection but carry Arc's NotAudited metadata.
// Like secret-only concepts, they cannot move into the Arc-free public event contracts project.
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
        _plan = Plan(_model);
        var name = _model.Application.Concepts.Single(candidate => candidate.Id == target).Name;
        _concept = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == $"Common/{name}.cs").Bytes.AsSpan());
    }

    [Fact] void should_keep_the_concept_out_of_the_contracts_project() => _plan.Artifacts.ShouldNotContain(artifact => artifact.RelativePath.StartsWith("Contracts.Events.Contracts/", StringComparison.Ordinal));
    [Fact] void should_report_the_public_event_contract_blocker() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("carries Arc metadata (NotAudited) the contracts project cannot reference", StringComparison.Ordinal));
    [Fact] void should_preserve_both_classifications_with_pii_protection() => _concept.ShouldContain("[PII(OperationalSecret = true)]");
    [Fact] void should_render_the_arc_metadata() => _concept.ShouldContain("[NotAudited]");
    [Fact] void should_not_add_another_protection_handler() => _concept.ShouldNotContain("Encrypted");
    [Fact] void should_compile_the_concept_without_errors() => RenderedOutput.Errors([new RenderedFile("PersonalSecret.cs", _concept)]).ShouldBeEmpty();
}
#endif
