// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_undeclared_enumeration_value : a_stored_enumeration_event
{
    [Theory]
    [InlineData("\"unknown\"")]
    [InlineData("\"Issued\"")]
    [InlineData("\"1\"")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void should_refuse_any_value_other_than_a_member_number_or_declared_name(string value)
    {
        Prepare("1");
        UseStoredValue(value);
        var error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_stored], 0));
        error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    }
}
