// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_orphan_child_events;

public class and_a_child_joins_on_the_event : given.an_orphan_child_projection
{
    protected override string RootOperation => "";

    protected override string ChildOperation => "          join labels on code\n            with Placed\n              label = other";

    SemanticSpecificationRunRecord _result = null!;

    async Task Because() => _result = await Run();

    [Fact]
    void should_refuse_by_name_instead_of_dropping_the_other_operation()
    {
        _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        _result.Unsupported!.Details.ShouldContain("missing child parent");
    }
}
