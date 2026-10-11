// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_numeric_enumeration_value : a_stored_enumeration_event
{
    SemanticWorld _world = null!;

    void Establish() => Prepare("1");
    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_stored], 0);

    [Fact] void should_read_chronicles_integer_storage_form() => _converted.ShouldEqual(1);
    [Fact] void should_restore_the_declared_member_name() => _world.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("issued"));
}
