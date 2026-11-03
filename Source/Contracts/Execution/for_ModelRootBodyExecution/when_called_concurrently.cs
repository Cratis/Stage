// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_called_concurrently : given.a_model_root
{
    async Task Because()
    {
        Create();
        await using var original = await Provider();
        fingerprint = "changed";
        var providers = await Task.WhenAll(Provider().AsTask(), Provider().AsTask());
        foreach (var provider in providers) await provider.DisposeAsync();
    }

    [Fact] public async Task should_compile_a_changed_fingerprint_once() => await compiler.Received(2).CompileAsync(root, Arg.Any<CancellationToken>());
}
