// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class without_a_declared_source_identifier_and_with_ambiguous_producers : given.a_routed_event
{
    Exception? _error;

    void Establish() => _plan = compiled_plan.From(with_multiple_source_identifier_types.MultipleSourceModel.Replace("  identifier AccountId\n", string.Empty, StringComparison.Ordinal));

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], 0));

    [Fact] void should_refuse_the_ambiguous_fallback_across_all_producers() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
