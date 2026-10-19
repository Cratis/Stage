// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.when_performing_observable_queries;

public class and_the_query_is_not_observable : live_projects
{
    object? _result;

    async Task Because()
    {
        await Register("project-1", "Screenplay");
        _result = await Perform("AllProjects");
    }

    [Fact] void should_answer_once_with_the_collection() => Names((object[])_result!).ShouldContainOnly("project-1");
}
