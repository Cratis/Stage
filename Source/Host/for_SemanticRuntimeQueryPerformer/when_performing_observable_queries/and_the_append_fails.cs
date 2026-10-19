// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.when_performing_observable_queries;

public class and_the_append_fails : live_projects
{
    IDisposable _subscription = null!;
    SemanticExecutionResult _outcome = null!;
    Exception? _error;

    async Task Because()
    {
        var result = await Perform("LiveProjects");
        var emissions = Observe((IObservable<IEnumerable<Api.DynamicReadModel>>)result!, out _subscription);
        await Next(emissions);
        _log.BeforeAppend = () => throw new IOException("The event store went away.");
        _outcome = await TryRegister("project-1", "Screenplay");
        // A channel completed with an error surfaces that error as the inner exception of its closed read.
        _error = (await Catch.Exception(() => Next(emissions)))?.InnerException;
    }

    void Destroy() => _subscription.Dispose();

    [Fact] void should_fault_the_runtime() => _outcome.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_fail_the_subscription_instead_of_keeping_the_stale_answer() => _error.ShouldBeOfExactType<SemanticQueryUnsupported>();
}
