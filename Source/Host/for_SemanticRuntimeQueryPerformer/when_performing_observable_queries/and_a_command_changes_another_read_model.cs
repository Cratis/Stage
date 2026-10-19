// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.when_performing_observable_queries;

public class and_a_command_changes_another_read_model : live_projects
{
    IEnumerable<object> _afterTheTag = null!;
    IDisposable _subscription = null!;

    async Task Because()
    {
        var subject = (IObservable<IEnumerable<DynamicReadModel>>)(await Perform("LiveProjects"))!;
        var emissions = Observe(subject, out _subscription);
        await Next(emissions);

        // Nothing is emitted for the tag, so the very next emission is the one the registration causes.
        await Tag("tag-1", "Urgent");
        await Register("project-1", "Screenplay");
        _afterTheTag = await Next(emissions);
    }

    void Destroy() => _subscription.Dispose();

    [Fact] void should_stay_silent_for_the_unrelated_change() => Names(_afterTheTag).ShouldContainOnly("project-1");
}
