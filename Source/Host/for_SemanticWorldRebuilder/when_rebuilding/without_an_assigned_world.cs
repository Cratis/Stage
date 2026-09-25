// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class without_an_assigned_world : a_rebuildable_world
{
    Exception? _error;

    async Task Because()
    {
        var services = new ServiceCollection();
        SemanticRuntimeHosting.Add(services, _plan, SemanticHost.WorldProvider(() => null));
        services.AddSingleton(Substitute.For<IAppendSemanticFacts>());
        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<ISemanticRuntime>();
        _error = await Catch.Exception(() => runtime.ReadModels(_readModel.Id));
    }

    [Fact] void should_refuse_premature_world_use() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
