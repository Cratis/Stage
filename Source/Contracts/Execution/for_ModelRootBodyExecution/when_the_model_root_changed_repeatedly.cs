// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_the_model_root_changed_repeatedly : given.a_model_root
{
    [Fact]
    async Task should_retain_only_slots_that_still_have_leases()
    {
        execution = new(root, () => compiler, _ => Substitute.For<IBodyWorkerClient, IAsyncDisposable>(), _ => fingerprint);
        var held = await Provider();
        for (var revision = 0; revision < 5; revision++)
        {
            fingerprint = $"revision-{revision}";
            await (await Provider()).DisposeAsync();
        }

        fingerprint = "final";
        await using var current = await Provider();
        execution.RetainedSlots.ShouldEqual(2);
        await held.DisposeAsync();
    }
}
