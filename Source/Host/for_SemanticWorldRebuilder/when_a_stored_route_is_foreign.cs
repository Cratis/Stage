// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder;

public class when_a_stored_route_is_foreign : given.a_routed_event
{
    Exception? _error;

    void Establish()
    {
        _plan = compiled_plan.From(a_routed_runtime.Source.Replace("module Banking", "  stream Statements\nmodule Banking", StringComparison.Ordinal));
        _event.Context.EventStreamType = "Statements";
    }

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], 0));

    [Fact] void should_refuse_reconstruction() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
