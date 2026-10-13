// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_identities_are_provisional : given.a_release_baseline
{
    void Because()
    {
        var documents = SemanticDocumentSet.Create(_workspace.Compilation.Value!.Documents.Documents, SemanticIdentityCatalog.Empty(_workspace.IdentityCatalog.Application));
        var compilation = new SemanticModelCompiler().Compile("RecordsApp", documents).Value!;
        _assessment = StageReleaseBaseline.Check(compilation, _baseline);
    }
    [Fact] void should_report_unavailable() => _assessment.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unavailable);
    [Fact] void should_not_guess_identity_matches() => _assessment.Violations.ShouldBeEmpty();
}
#endif
