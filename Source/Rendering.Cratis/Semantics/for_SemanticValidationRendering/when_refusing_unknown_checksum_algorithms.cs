// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering;

public class when_refusing_unknown_checksum_algorithms
{
    [Theory]
    [InlineData("unknown")]
    [InlineData("Luhn")]
    [InlineData("None")]
    [InlineData("")]
    public void should_not_emit_an_unrecognized_algorithm(string algorithm) => SemanticValidationRendering.CanRender(
        new(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text(algorithm), null)).ShouldBeFalse();
}
