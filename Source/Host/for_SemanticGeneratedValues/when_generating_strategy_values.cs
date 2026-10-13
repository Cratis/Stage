// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        Enumerable.Range(0, 256).Select(_ => SemanticGeneratedValues.New(concept, SemanticVersion.V15))
            .All(value => SemanticGenerationShapes.Matches(concept, SemanticValue.Text(value))).ShouldBeTrue();
    }
}
