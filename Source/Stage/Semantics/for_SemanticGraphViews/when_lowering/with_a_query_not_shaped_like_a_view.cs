// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticGraphViews.when_lowering;

public class with_a_query_not_shaped_like_a_view : given.an_organization_with_views
{
    Exception? _error;

    void Because()
    {
        var query = Query("GroupsWithMembers");
        var match = query.Matches[0] with { Hops = [new("membership", "MEMBER_OF", false, new("member", "Person"))] };
        _error = Catch.Exception(() => SemanticGraphViews.Lower(_application, query with { Matches = [match] }, ReadModelOf(query), new Dictionary<string, object?>()));
    }

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<UnsupportedSemanticGraph>();
}
