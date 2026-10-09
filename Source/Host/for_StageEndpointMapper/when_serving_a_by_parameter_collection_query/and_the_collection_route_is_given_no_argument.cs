// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper.when_serving_a_by_parameter_collection_query;

public class and_the_collection_route_is_given_no_argument : given.a_comment_thread
{
    async Task Because() => _response = await Request("GET", CollectionRoute);

    [Fact] void should_succeed() => _response.Status.ShouldEqual(200);
    [Fact] void should_keep_returning_every_comment() => Texts().ShouldContainOnly([CommentForA, CommentForB]);
}
