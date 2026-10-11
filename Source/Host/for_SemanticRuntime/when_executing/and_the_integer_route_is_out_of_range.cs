// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_the_integer_route_is_out_of_range : given.a_whole_number_route
{
    async Task Because() => await Record(9007199254740992L);

    [Fact] void should_reject_the_contract() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_append_nothing() => _appended.ShouldBeNull();
    [Fact] void should_commit_no_world() => _result.World.Facts.ShouldBeEmpty();
}
