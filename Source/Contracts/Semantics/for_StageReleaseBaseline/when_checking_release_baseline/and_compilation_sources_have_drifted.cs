// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_compilation_sources_have_drifted : given.a_release_baseline
{
    void Because()
    {
        var compilation = _workspace.Compilation.Value!;
        var documents = SemanticDocumentSet.Create([.. compilation.Documents.Documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.DisplayPath, document.Text.Replace("value String", "value Bool", StringComparison.Ordinal)))], compilation.Documents.IdentityCatalog);
        _assessment = StageReleaseBaseline.Check(SemanticCompilation.Create(compilation.Model, documents, SemanticSourceMap.Create([], documents.Documents)), _baseline);
    }
    [Fact] void should_report_unavailable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unavailable);
    [Fact] void should_not_report_violations_for_a_different_revision() => _assessment.Violations.ShouldBeEmpty();
}
#endif
