// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ScreenplayEventModelVisitor;

public class when_refusing_uuid_comparisons : Specification
{
    [Theory]
    [InlineData("Uuid", "require member != other")]
    [InlineData("MemberId", "require member == other")]
    [InlineData("Uuid", "member == other")]
    [InlineData("Uuid", "member != other")]
    [InlineData("MemberId", "member == other")]
    [InlineData("MemberId", "member != other")]
    [InlineData("Uuid", "member == \"a1111111-1111-4111-8111-111111111111\"")]
    [InlineData("MemberId", "require member != \"a1111111-1111-4111-8111-111111111111\"")]
    [InlineData("MemberId", "production")]
    public void should_refuse_with_STAGE_ESM_049(string type, string clause)
    {
        var source = $$"""
            concept MemberId : Uuid
            module Access
              feature Approvals
                slice StateChange Keep
                  command Keep
                    id String identifier
                    member {{type}}
                    other {{type}}
            {{(clause == "production" ? "        produces when member == other\n          Kept\n            for id" : $"        validate\n          {clause}\n        produces Kept\n          for id")}}
                  event Kept
            """;
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join(';', parsed.Diagnostics));
        var error = Assert.Throws<InvalidEventModel>(() => ScreenplayEventModelVisitor.EnsureBehaviorSupported(parsed.Value!));
        error.Message.ShouldContain("STAGE-ESM-049");
        error.Message.ShouldContain("'Keep'");
        error.Message.ShouldContain("'member'");
        Assert.Throws<InvalidEventModel>(() => new ScreenplayEventModelVisitor().Visit(parsed.Value!));
    }

    [Theory]
    [InlineData("Uuid", "require member == other")]
    [InlineData("MemberId", "require member == other")]
    [InlineData("Uuid", "member == other")]
    [InlineData("Uuid", "member != other")]
    [InlineData("MemberId", "member == other")]
    [InlineData("MemberId", "member != other")]
    [InlineData("Uuid", "production")]
    [InlineData("MemberId", "production")]
    public void should_refuse_a_uuid_property_only_on_the_right(string type, string clause)
    {
        var source = $$"""
            concept MemberId : Uuid
            module Access
              feature Approvals
                slice StateChange Keep
                  command Keep
                    id String identifier
                    member String
                    other {{type}}
            {{(clause == "production" ? "        produces when member == other\n          Kept\n            for id" : $"        validate\n          {clause}\n        produces Kept\n          for id")}}
                  event Kept
            """;
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join(';', parsed.Diagnostics));
        var error = Assert.Throws<InvalidEventModel>(() => ScreenplayEventModelVisitor.EnsureBehaviorSupported(parsed.Value!));
        error.Message.ShouldContain("STAGE-ESM-049");
        error.Message.ShouldContain("'other'");
        Assert.Throws<InvalidEventModel>(() => new ScreenplayEventModelVisitor().Visit(parsed.Value!));
    }

    [Fact]
    public void should_keep_admitting_models_without_uuid_comparisons()
    {
        const string source = "concept MemberId : Uuid\nmodule Access\n  feature Approvals\n    slice StateChange Keep\n      command Keep\n        id String identifier\n        member MemberId\n        produces Kept\n          for id\n      event Kept";
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join(';', parsed.Diagnostics));
        ScreenplayEventModelVisitor.EnsureBehaviorSupported(parsed.Value!);
    }
}
