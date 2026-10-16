// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ScreenplayEventModelVisitor;

public class when_refusing_date_comparisons : Specification
{
    [Theory]
    [InlineData("Date", "at >= \"2026-01-01\"")]
    [InlineData("DateTime", "require at == from")]
    [InlineData("Moment", "require at >= from")]
    [InlineData("Date", "production")]
    [InlineData("Moment", "production")]
    public void should_refuse_before_legacy_engines_drop_or_misinterpret_the_comparison(string type, string clause)
    {
        var source = $$"""
            concept Moment : DateTime
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    from {{type}}
                    at {{type}}
            {{(clause == "production" ? "        produces when at >= from\n          Kept\n            for id" : $"        validate\n          {clause}\n        produces Kept\n          for id")}}
                  event Kept
            """;
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join(';', parsed.Diagnostics));
        var error = Assert.Throws<InvalidEventModel>(() => ScreenplayEventModelVisitor.EnsureBehaviorSupported(parsed.Value!));
        error.Message.ShouldContain("STAGE-ESM-046");
        error.Message.ShouldContain("'at'");
        Assert.Throws<InvalidEventModel>(() => new ScreenplayEventModelVisitor().Visit(parsed.Value!));
    }

    [Fact]
    public void should_preserve_the_documented_today_omission()
    {
        const string source = "module Scheduling\n  feature Windows\n    slice StateChange Keep\n      command Keep\n        dueDate Date\n        validate\n          dueDate > today";
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join(';', parsed.Diagnostics));
        ScreenplayEventModelVisitor.EnsureBehaviorSupported(parsed.Value!);
    }
}
