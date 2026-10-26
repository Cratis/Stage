// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation;

public class when_a_compiler_invocation_throws : Specification
{
    readonly artifact_compilations<int> _cache = new();
    readonly InvalidOperationException _failure = new("Compiler could not start.");
    Task<int> _first = null!;
    Task<int> _second = null!;
    Exception[] _observed = [];

    async Task Because()
    {
        _first = _cache.Get([PlannedArtifact.CreateText("App.cs", "class App;")], () => throw _failure);
        _second = _cache.Get([PlannedArtifact.CreateText("App.cs", "class App;")], () => Task.FromResult(0));
        _observed = [await Assert.ThrowsAsync<InvalidOperationException>(() => _first), await Assert.ThrowsAsync<InvalidOperationException>(() => _second)];
    }

    [Fact]
    void should_cache_the_failure_without_retrying_or_replacing_it_with_success()
    {
        Assert.Same(_first, _second);
        Assert.All(_observed, exception => Assert.Same(_failure, exception));
        Assert.Equal(1, _cache.Invocations);
        Assert.Equal(2, _cache.Requests);
    }
}
#endif
