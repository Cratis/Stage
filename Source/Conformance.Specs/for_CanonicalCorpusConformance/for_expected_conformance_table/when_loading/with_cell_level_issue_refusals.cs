// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_expected_conformance_table.when_loading;

public class with_cell_level_issue_refusals : Specification
{
    expected_conformance_table _table = null!;

    void Because() => _table = expected_conformance_table.Load("""{"corpusVersion":"1.0.0","minimumVectorCount":1,"excludedTypes":{},"vectors":{"V1":{"renderer":{"outcome":"refused", "issueRefusals": []}}}}""");

    [Fact] void should_reject_the_schema() => _table.SchemaProblems.Any(problem => problem.Contains("legacy cell-level", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
