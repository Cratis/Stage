// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_out_of_range_enumeration_number : a_stored_enumeration_event
{
    [Theory]
    [InlineData("-1")]
    [InlineData("2")]
    [InlineData("2147483648")]
    public void should_refuse_a_number_without_a_declared_member(string number)
    {
        Prepare("1");
        UseStoredValue(number);
        var error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_stored], 0));
        error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    }
}
