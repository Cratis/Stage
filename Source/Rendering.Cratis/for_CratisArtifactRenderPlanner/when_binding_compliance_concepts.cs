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
    string[] _codes = [];

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Compliance"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("compliance"), "compliance", "Compliance.play", compliance_concepts.Source);
        var result = new SemanticModelCompiler().Compile("Compliance", SemanticDocumentSet.Create([document], catalog));
        _bound = result.Success;
        _codes = [.. result.Diagnostics.Select(diagnostic => diagnostic.Code)];
    }

    [Fact] void should_not_admit_syntax_only_compliance_metadata_to_the_semantic_renderer() => _bound.ShouldBeFalse();
    [Fact] void should_retain_the_binders_compliance_refusal() => _codes.ShouldContain("PLAY0268");
}
