// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_stored_caller : a_rebuildable_world
{
    SemanticFact _fact = null!;

    void Because()
    {
        _event.Context.CausedBy = new() { Subject = "kari", Name = "Kari Nordmann", UserName = "kari.nordmann" };
        _fact = SemanticWorldRebuilder.Fact(_plan, _event, false);
    }

    [Fact] void should_read_the_stored_subject() => _fact.CausedBy!.Subject.ShouldEqual("kari");
    [Fact] void should_read_the_stored_name() => _fact.CausedBy!.Name.ShouldEqual("Kari Nordmann");
    [Fact] void should_read_the_stored_user_name() => _fact.CausedBy!.UserName.ShouldEqual("kari.nordmann");
}
