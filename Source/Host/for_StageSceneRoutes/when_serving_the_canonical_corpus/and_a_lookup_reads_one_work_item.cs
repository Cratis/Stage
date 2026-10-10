// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_serving_the_canonical_corpus;

public class and_a_lookup_reads_one_work_item : given.the_canonical_corpus
{
    (int Status, string Body, string? EndpointName) _withArgument;
    (int Status, string Body, string? EndpointName) _withoutArgument;

    async Task Because()
    {
        _withArgument = await Request("GET", $"{_queryRoutes["WorkItemById"]}?workItemId={WorkItemB}");
        _withoutArgument = await Request("GET", _queryRoutes["WorkItemById"]);
    }

    [Fact] void should_resolve_the_query_to_its_own_route() => _queryRoutes["WorkItemById"].ShouldEqual("/api/workspaces/tracking/work-item-by-id");
    [Fact] void should_return_the_work_item_its_argument_names() => Values(_withArgument, "title").ShouldContainOnly([TitleB]);
    [Fact] void should_answer_without_its_argument() => _withoutArgument.Status.ShouldEqual(200);
    [Fact] void should_return_nothing_without_its_argument() => Values(_withoutArgument, "title").ShouldBeEmpty();
}
