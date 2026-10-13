// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Concepts;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticGeneratedValues;

public class when_generating_strategy_values : Specification
{
    [Theory]
    [InlineData("Uuid", "uuidv7")]
    [InlineData("String", "ulid")]
    [InlineData("String", "typeid \"user\"")]
    public void should_encode_the_context_time_in_milliseconds(string primitive, string strategy)
    {
        var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);
        var plan = compiled_plan.From($"concept Identity : {primitive}\n  generate {strategy}\nmodule Work\n  feature Creation\n    slice StateChange Start\n      event Started");
        var value = SemanticGeneratedValues.New(plan.Model.Application.Concepts.Single(), SemanticVersion.V15, new(instant, GenerationContext.DefaultTenant));
        var milliseconds = strategy == "uuidv7"
            ? long.Parse(value[..8] + value[9..13], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : (strategy.StartsWith("typeid", StringComparison.Ordinal) ? value[5..15] : value[..10]).Aggregate(0L, (time, symbol) => (time << 5) + "0123456789ABCDEFGHJKMNPQRSTVWXYZ".IndexOf(char.ToUpperInvariant(symbol)));
        milliseconds.ShouldEqual(instant.ToUnixTimeMilliseconds());
    }

    [Theory]
    [InlineData("Uuid", "uuid")]
    [InlineData("Uuid", "uuidv7")]
    [InlineData("String", "uuid case upper prefix \"ref_\"")]
    [InlineData("String", "uuidv7 case lower prefix \"ref_\"")]
    [InlineData("String", "ulid")]
    [InlineData("String", "ulid case lower")]
    [InlineData("String", "typeid \"user\"")]
    [InlineData("String", "typeid \"\"")]
    [InlineData("String", "nanoid")]
    [InlineData("String", "nanoid 64 alphabet \"ab\" prefix \"code_\"")]
    [InlineData("String", "bytes 32 base64url")]
    [InlineData("String", "bytes 16 hex case upper")]
    [InlineData("String", "bytes 16 base32 case lower")]
    public void should_generate_values_matching_the_independent_encoding_shape(string primitive, string strategy)
    {
        var plan = compiled_plan.From($"concept Identity : {primitive}\n  generate {strategy}\nmodule Work\n  feature Creation\n    slice StateChange Start\n      event Started");
        var concept = plan.Model.Application.Concepts.Single();
        Enumerable.Range(0, 256).Select(_ => SemanticGeneratedValues.New(concept, SemanticVersion.V15, new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero), GenerationContext.DefaultTenant)))
            .All(value => SemanticGenerationShapes.Matches(concept, SemanticValue.Text(value))).ShouldBeTrue();
    }
}
