// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class without_a_declared_source_identifier : given.a_routed_event
{
    SemanticWorld _world = null!;

    void Establish() => _plan = compiled_plan.From(a_routed_runtime.Source.Replace("  identifier AccountId\n", string.Empty, StringComparison.Ordinal));

    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_event], 0);

    [Fact] void should_fall_back_to_the_unambiguous_producer_type() => _world.Facts.Single().Context!.EventSource.Type.ShouldEqual(_plan.Commands.Values.Single().Properties.Single(property => property.IsIdentifier).Type);
}
