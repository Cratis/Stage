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

    void Because()
    {
        var services = new ServiceCollection();
        SemanticRuntimeHosting.Add(services, _plan, SemanticHost.WorldProvider(() => null));
        services.AddSingleton(Substitute.For<IAppendSemanticFacts>());
        using var provider = services.BuildServiceProvider();
        _error = Catch.Exception(() => provider.GetRequiredService<ISemanticRuntime>());
    }

    [Fact] void should_refuse_premature_runtime_resolution() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
