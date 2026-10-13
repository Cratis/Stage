// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_GenerationSyntaxAdmission;

public class when_visiting_a_generation_strategy : Specification
{
    [Theory]
    [InlineData("Uuid", "uuid")]
    [InlineData("Uuid", "uuidv7")]
    [InlineData("String", "ulid")]
    [InlineData("String", "typeid \"user\"")]
    [InlineData("String", "nanoid 64 alphabet \"ab\"")]
    [InlineData("String", "bytes 16 hex")]
    public void should_refuse_before_legacy_translation(string primitive, string strategy)
    {
        var parsed = new ScreenplayCompiler().Parse($"concept Identity : {primitive}\n  generate {strategy}\nmodule Work\n  feature Creation\n    slice StateChange Start\n      event Started");
        parsed.Success.ShouldBeTrue();
        var error = Catch.Exception(() => new GenerationSyntaxAdmission().VisitApplication(parsed.Value!));
        error.ShouldBeOfExactType<UnsupportedGenerationStrategy>();
        error.Message.StartsWith("STAGE-ESM-028:", StringComparison.Ordinal).ShouldBeTrue();
        ((UnsupportedGenerationStrategy)error).Location.Line.ShouldEqual(1);
        Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(parsed.Value!)).ShouldBeOfExactType<UnsupportedGenerationStrategy>();
    }

    [Fact]
    public void should_keep_implicit_uuid_legacy_behavior()
    {
        var parsed = new ScreenplayCompiler().Parse("concept Identity : Uuid\nmodule Work\n  feature Creation\n    slice StateChange Start\n      event Started");
        parsed.Success.ShouldBeTrue();
        Catch.Exception(() => new GenerationSyntaxAdmission().VisitApplication(parsed.Value!)).ShouldBeNull();
    }
}
