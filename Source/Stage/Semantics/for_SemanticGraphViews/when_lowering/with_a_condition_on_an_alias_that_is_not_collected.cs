// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.when_lowering;

public class with_a_condition_on_an_alias_that_is_not_collected : given.an_organization_with_views
{
    Exception? _error;

    void Because()
    {
        var query = Query("GroupsWithMembers");
        var refused = query with { Conditions = [new("membership", "since") { Argument = "since" }] };
        _error = Catch.Exception(() => SemanticGraphViews.Lower(_application, refused, ReadModelOf(query), new Dictionary<string, object?> { ["since"] = "2026-01-01" }));
    }

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<UnsupportedSemanticGraph>();
}
