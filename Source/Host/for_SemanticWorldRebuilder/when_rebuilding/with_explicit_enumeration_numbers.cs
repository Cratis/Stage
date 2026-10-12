// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_explicit_enumeration_numbers : a_stored_enumeration_event
{
    void Establish() => Prepare("90", numbered: true);

    [Theory]
    [InlineData("90", "issued")]
    [InlineData("-1", "draft")]
    [InlineData("\"draft\"", "draft")]
    public void should_restore_the_member_by_identity_not_position(string stored, string member)
    {
        UseStoredValue(stored);
        var world = SemanticWorldRebuilder.Create(_plan, [_stored], 0);
        world.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text(member));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("91")]
    [InlineData("90.5")]
    [InlineData("\"undeclared\"")]
    public void should_refuse_values_without_a_declared_member(string stored)
    {
        UseStoredValue(stored);
        Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_stored], 0)).ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    }
}
