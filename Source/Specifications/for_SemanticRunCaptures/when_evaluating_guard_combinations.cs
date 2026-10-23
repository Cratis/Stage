// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticRunCaptures;

public class when_evaluating_guard_combinations : Specification
{
    [Theory]
    [InlineData("$.status == \"delivered\" && $.label == \"box\"", true)]
    [InlineData("$.status == \"sent\" || $.label == \"box\"", true)]
    [InlineData("!($.status == \"sent\") && $.count >= 2", true)]
    [InlineData("$.count < 2 || $.status == \"sent\"", false)]
    [InlineData("($.status == \"delivered\" && $.count == 2) || false", true)]
    public void should_use_the_reference_guard_grammar(string text, bool expected)
    {
        SemanticRunCaptureExpression.TryParse(text, out var expression).ShouldBeTrue();
        var fields = new Dictionary<string, SemanticValue>
        {
            ["status"] = SemanticValue.Text("delivered"),
            ["label"] = SemanticValue.Text("box"),
            ["count"] = SemanticValue.Number(2)
        };
        var evaluated = SemanticRunCaptureExpression.Evaluate(expression, field => new(fields.GetValueOrDefault(field), null));
        evaluated.Unsupported.ShouldBeNull();
        evaluated.Holds.ShouldEqual(expected);
    }

    [Theory]
    [InlineData("false && $.unsupported", false)]
    [InlineData("true || $.unsupported", true)]
    public void should_short_circuit_without_reading_an_unsupported_field(string text, bool expected)
    {
        SemanticRunCaptureExpression.TryParse(text, out var expression).ShouldBeTrue();
        var evaluated = SemanticRunCaptureExpression.Evaluate(expression, _ => new(null, "Unsupported field"));
        evaluated.Unsupported.ShouldBeNull();
        evaluated.Holds.ShouldEqual(expected);
    }
}
