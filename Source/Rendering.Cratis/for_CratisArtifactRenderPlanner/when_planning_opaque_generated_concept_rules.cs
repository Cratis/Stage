// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_opaque_generated_concept_rules : Specification
{
    ExecutableSemanticModel _model = null!;
    SemanticConcept _concept = null!;
    SemanticSpecification _specification = null!;
    ArtifactRenderPlan _result = null!;
    ImmutableArray<SemanticImplementationRequirement> _requirements;
    ImmutableDictionary<string, string> _contents = null!;
    ImmutableArray<SemanticTypedContextDescriptor> _descriptors;

    void Establish()
    {
        var corpus = GeneratedConceptRulesCorpus.V20;
        var form = corpus.SourceForms.Single(form => form.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        compilation.Success.ShouldBeTrue();
        _model = SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan());
        _model.Revision.ShouldEqual(compilation.Value!.Model.Revision);
        _requirements = compilation.ImplementationRequirements;
        _contents = SemanticImplementationBodies.Resolve(compilation.Value.Documents, _requirements);
        _descriptors = compilation.TypedContextDescriptors;
        _concept = _model.Application.Concepts.Single(concept => concept.Name == "ReviewedCode");
        _specification = _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Review").Specifications.Single();
    }

    void Because() => _result = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("GeneratedConceptRules", "GeneratedConceptRules"), _requirements, _contents, [], _descriptors);

    [Fact] void should_refuse_the_plan_without_throwing() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_publish_partial_artifacts() => _result.Artifacts.ShouldBeEmpty();
    [Fact] void should_keep_the_concept_refusal() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Artifact == _concept.Id);
    [Fact] void should_refuse_the_pinned_specification_with_the_concept_and_rule() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Artifact == _specification.Id && diagnostic.Message.Contains(_concept.Name, StringComparison.Ordinal) && _concept.Validations.All(rule => diagnostic.Message.Contains(rule.RequirementId.ToString(), StringComparison.Ordinal)));
}
