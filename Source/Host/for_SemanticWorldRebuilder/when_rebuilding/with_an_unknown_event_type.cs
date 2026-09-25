// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_unknown_event_type : a_rebuildable_world
{
    Exception? _error;

    void Because()
    {
        _event.Context.EventType.Id = "UnknownEvent";
        _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], _mirror, 0));
    }

    [Fact] void should_refuse_with_the_unknown_type() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_unknown_type() => _error!.Message.ShouldContain("UnknownEvent");
}
