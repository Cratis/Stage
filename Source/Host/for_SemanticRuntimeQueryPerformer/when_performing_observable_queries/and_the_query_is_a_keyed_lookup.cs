// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.when_performing_observable_queries;

public class and_the_query_is_a_keyed_lookup : live_projects
{
    DynamicReadModel? _before;
    DynamicReadModel? _after;
    IDisposable _subscription = null!;

    async Task Because()
    {
        var subject = (IObservable<DynamicReadModel?>)(await Perform("LiveProject", new QueryArguments { ["projectId"] = "project-1" }))!;
        var emissions = Observe(subject, out _subscription);
        _before = await Next(emissions);
        await Register("project-1", "Screenplay");
        _after = await Next(emissions);
    }

    void Destroy() => _subscription.Dispose();

    [Fact] void should_emit_nothing_for_a_missing_instance() => _before.ShouldBeNull();
    [Fact] void should_emit_the_instance_once_it_exists() => _after!.Id.ShouldEqual("project-1");
}
