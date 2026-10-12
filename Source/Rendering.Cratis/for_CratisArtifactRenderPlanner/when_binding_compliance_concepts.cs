// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_binding_compliance_concepts : Specification
{
    bool _bound;
    SemanticVersion _version;
    string[] _codes = [];

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Compliance"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("compliance"), "compliance", "Compliance.play", compliance_concepts.Source);
        var result = new SemanticModelCompiler().Compile("Compliance", SemanticDocumentSet.Create([document], catalog));
        _bound = result.Success;
        _version = result.Value!.Model.SemanticVersion;
        _codes = [.. result.Diagnostics.Select(diagnostic => diagnostic.Code)];
    }

    [Fact] void should_admit_compliance_markers_to_the_semantic_model() => _bound.ShouldBeTrue();
    [Fact] void should_select_v13() => _version.ShouldEqual(SemanticVersion.V13);
    [Fact] void should_keep_reasons_as_authoring_warnings() => _codes.ShouldContain("PLAY0270");
    [Fact] void should_not_report_the_old_blanket_refusal() => _codes.ShouldNotContain("PLAY0268");
}
