// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_an_unmarked_model : Specification
{
    ArtifactRenderPlan _marked = null!;
    ArtifactRenderPlan _unmarked = null!;
    ExecutableSemanticModel _model = null!;
    ExecutableSemanticModel _markedModel = null!;
    readonly List<string> _differences = [];

    void Establish()
    {
        var form = ConceptComplianceCorpus.V13.SourceForms.Single(form => form.Name == "single");
        _markedModel = a_concept_compliance_model.Compile(form);
        _marked = invoice_model.Plan(_markedModel);
        _model = a_concept_compliance_model.Compile(form, false);
    }

    void Because()
    {
        _unmarked = invoice_model.Plan(_model);
        var markedArtifacts = QualifiedCommonOutput.Restore(_markedModel, _marked, new("InvoiceApp", "Invoices"));
        foreach (var artifact in QualifiedCommonOutput.Restore(_model, _unmarked, new("InvoiceApp", "Invoices")))
        {
            var marked = markedArtifacts.Single(candidate => candidate.RelativePath == artifact.RelativePath);
            var expected = string.Join('\n', Encoding.UTF8.GetString(marked.Bytes.AsSpan()).Replace("[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute] ", string.Empty, StringComparison.Ordinal).Split('\n').Where(line => !line.Contains("PIIAttribute]", StringComparison.Ordinal) && !line.Contains("EncryptedAttribute(", StringComparison.Ordinal) && !line.Contains("NotAuditedAttribute]", StringComparison.Ordinal)));
            if (Encoding.UTF8.GetString(artifact.Bytes.AsSpan()) != expected) _differences.Add(artifact.RelativePath);
        }
    }

    [Fact] void should_keep_the_unmarked_model_on_v12() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V12);
    [Fact] void should_admit_the_unmarked_model() => _unmarked.Diagnostics.ShouldBeEmpty();
    [Fact] void should_render_the_same_artifact_set() => _unmarked.Artifacts.Select(artifact => artifact.RelativePath).ShouldContainOnly(_marked.Artifacts.Select(artifact => artifact.RelativePath));
    [Fact] void should_change_only_the_compliance_attributes() => _differences.ShouldBeEmpty();
}
