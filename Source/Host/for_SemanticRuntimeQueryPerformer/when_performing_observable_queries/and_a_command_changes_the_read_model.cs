// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.when_performing_observable_queries;

public class and_a_command_changes_the_read_model : live_projects
{
    object? _result;
    IEnumerable<object> _current = null!;
    IEnumerable<object> _updated = null!;
    IDisposable _subscription = null!;

    async Task Because()
    {
        _result = await Perform("LiveProjects");
        var emissions = Observe((IObservable<IEnumerable<Api.DynamicReadModel>>)_result!, out _subscription);
        _current = await Next(emissions);
        await Register("project-1", "Screenplay");
        _updated = await Next(emissions);
    }

    void Destroy() => _subscription.Dispose();

    [Fact] void should_answer_with_an_observable_of_the_collection() => _result!.GetType().GetInterfaces().ShouldContain(typeof(ISubject<>).MakeGenericType(typeof(IEnumerable<>).MakeGenericType(_performer.ReadModelType)));
    [Fact] void should_emit_the_current_result_first() => _current.ShouldBeEmpty();
    [Fact] void should_emit_the_new_result_after_the_command() => Names(_updated).ShouldContainOnly("project-1");
}
