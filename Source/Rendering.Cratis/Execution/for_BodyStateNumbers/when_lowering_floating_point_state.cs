// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyStateNumbers;

public class when_lowering_floating_point_state : Specification
{
    SemanticNumberValue _double = null!;
    SemanticNumberValue _single = null!;
    Exception? _overflow;

    void Because()
    {
        _double = (SemanticNumberValue)BodyStateNumbers.Write(1.23d)!;
        _single = (SemanticNumberValue)BodyStateNumbers.Write(1.23f)!;
        _overflow = Catch.Exception(() => BodyStateNumbers.Write(double.PositiveInfinity));
    }

    [Fact] void should_preserve_the_double() => _double.Value.ShouldEqual(1.23m);
    [Fact] void should_preserve_the_existing_single_to_double_promotion() => _single.Value.ShouldEqual(1.23000001907349m);
    [Fact] void should_keep_overflow_rejection() => _overflow.ShouldBeOfExactType<OverflowException>();
}
