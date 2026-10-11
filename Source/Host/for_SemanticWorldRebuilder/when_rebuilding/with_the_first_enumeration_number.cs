// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_the_first_enumeration_number : a_stored_enumeration_event
{
    SemanticWorld _world = null!;

    void Establish() => Prepare("0");
    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_stored], 0);

    [Fact] void should_restore_the_first_declared_member() => _world.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("draft"));
}
