// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_ordinal_text_rules : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_admit_v24_with_required_or_optional_validated_concepts(bool optional)
    {
        var form = TextRulesCorpus.SourceForms.Single(form => form.Name == "first-class");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey),
            document.StableKey,
            document.DisplayPath,
            optional ? document.Text.Replace("reference Reference", "reference Reference?", StringComparison.Ordinal) : document.Text));
        var compilation = new SemanticModelCompiler().Compile("TextRules", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        model.LanguageVersion.ShouldEqual(LanguageVersion.V24);
        model.SemanticVersion.ShouldEqual(SemanticVersion.V24);
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("TextRules", "TextRules"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        plan.Artifacts.ShouldNotBeEmpty();
    }
}
