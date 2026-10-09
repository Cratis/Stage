// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_serving_the_canonical_corpus;

public class and_a_thread_reads_the_comments_of_one_work_item : given.the_canonical_corpus
{
    (int Status, string Body, string? EndpointName) _forB;
    (int Status, string Body, string? EndpointName) _withoutArgument;

    async Task Because()
    {
        _forB = await Request("GET", $"{_queryRoutes["CommentsForWorkItem"]}?workItemId={WorkItemB}");
        _withoutArgument = await Request("GET", _queryRoutes["CommentsForWorkItem"]);
    }

    [Fact] void should_resolve_the_query_to_its_own_route() => _queryRoutes["CommentsForWorkItem"].ShouldEqual("/api/workspaces/tracking/comments-for-work-item");
    [Fact] void should_bind_every_thread_declaration_to_that_route() => Reading("WorkItemDetails", "CommentsForWorkItem").Concat(Reading("CommentThread", "CommentsForWorkItem")).Select(component => component.Properties["route"]).Distinct().ShouldContainOnly(["/api/workspaces/tracking/comments-for-work-item"]);
    [Fact] void should_return_only_the_comment_of_the_named_work_item() => Values(_forB, "text").ShouldContainOnly([CommentForB]);
    [Fact] void should_return_nothing_without_its_argument() => Values(_withoutArgument, "text").ShouldBeEmpty();
}
