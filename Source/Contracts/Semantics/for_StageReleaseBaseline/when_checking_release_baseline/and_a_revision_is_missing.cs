// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_StageReleaseBaseline.when_checking_release_baseline;

public class and_a_revision_is_missing : given.a_release_baseline
{
    void Because()
    {
        var catalog = _workspace.IdentityCatalog;
        var retired = SemanticIdentityCatalog.Create(
            catalog.Application,
            catalog.Documents,
            [.. catalog.Semantics.Where(assignment => !assignment.Address.Parts.Any(part => part.Key == "Removed"))],
            [.. catalog.EventContracts.Where(assignment => assignment.Address.Name != "Removed")]);
        _assessment = StageReleaseBaseline.Check(Workspace(Source.Replace("      event Removed", string.Empty, StringComparison.Ordinal), retired), _baseline);
    }
    [Fact] void should_report_the_missing_revision() => _assessment.Violations.Single().Kind.ShouldEqual("BaselinedRevisionMissing");
    [Fact] void should_preserve_the_missing_stored_name() => _assessment.Violations.Single().StoredName.ShouldEqual("Removed");
    [Fact] void should_preserve_the_missing_revision() => _assessment.Violations.Single().Revision.ShouldEqual(1u);
}
#endif
