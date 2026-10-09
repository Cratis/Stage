// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper.when_serving_a_by_parameter_collection_query;

public class and_the_argument_is_empty : given.a_comment_thread
{
    async Task Because() => _response = await Request("GET", $"{KeyedRoute}?workItemId=");

    [Fact] void should_succeed() => _response.Status.ShouldEqual(200);
    [Fact] void should_return_an_empty_result() => Texts().ShouldBeEmpty();
    [Fact] void should_not_return_any_comment_of_parent_a() => _response.Body.ShouldNotContain(CommentForA);
    [Fact] void should_not_return_any_comment_of_parent_b() => _response.Body.ShouldNotContain(CommentForB);
}
