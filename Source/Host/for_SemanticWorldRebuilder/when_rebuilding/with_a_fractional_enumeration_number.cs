// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_fractional_enumeration_number : a_stored_enumeration_event
{
    Exception? _error;

    void Establish()
    {
        Prepare("1");
        UseStoredValue("1.5");
    }

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_stored], 0));

    [Fact] void should_refuse_a_fractional_member_number() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
