// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_disposed : given.a_model_root
{
    IBodyWorkerClient _next;
    ISemanticBodyExecutionProvider _first;
    ISemanticBodyExecutionProvider _second;
    Exception _error;

    async Task Because()
    {
        _next = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
        var count = 0;
        execution = new(root, () => compiler, _ => ++count == 1 ? worker : _next, _ => fingerprint);
        _first = await Provider();
        fingerprint = "changed";
        _second = await Provider();
        await execution.DisposeAsync();
        _error = await Catch.Exception(() => Provider().AsTask());
    }

    async Task Destroy()
    {
        await _first.DisposeAsync();
        await _second.DisposeAsync();
    }

    [Fact] public async Task should_close_the_current_worker() => await ((IAsyncDisposable)_next).Received(1).DisposeAsync();
    [Fact] public async Task should_close_the_retiring_worker() => await ((IAsyncDisposable)worker).Received(1).DisposeAsync();
    [Fact] public void should_reject_further_runs() => _error.ShouldBeOfExactType<ObjectDisposedException>();
}
