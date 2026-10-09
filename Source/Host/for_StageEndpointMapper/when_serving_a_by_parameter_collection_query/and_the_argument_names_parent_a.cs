// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper.when_serving_a_by_parameter_collection_query;

public class and_the_argument_names_parent_a : given.a_comment_thread
{
    async Task Because() => _response = await Request("GET", $"{KeyedRoute}?workItemId={ParentA}");

    [Fact] void should_succeed() => _response.Status.ShouldEqual(200);
    [Fact] void should_return_only_the_comment_of_parent_a() => Texts().ShouldContainOnly([CommentForA]);
}
