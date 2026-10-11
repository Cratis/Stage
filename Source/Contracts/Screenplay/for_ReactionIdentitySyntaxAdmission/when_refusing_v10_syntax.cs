// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ReactionIdentitySyntaxAdmission;

public class when_refusing_v10_syntax : Specification
{
    [Theory]
    [InlineData("reaction Follow\n        runs as system role \"Automation\"\n        when Started", "STAGE-ESM-034")]
    [InlineData("reaction Follow\n        from Account.Notes\n        when Started", "STAGE-ESM-035")]
    [InlineData("readmodel Snapshot\n        id String key\n      reducer Fold => Snapshot\n        from Account\n        on Started", "STAGE-ESM-035")]
    public void should_refuse_before_legacy_translation(string construct, string code)
    {
        var parsed = new ScreenplayCompiler().Parse("module Work\n  feature Completion\n    slice Automation Complete\n      event Started\n      " + construct);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        var error = Catch.Exception(() => new ReactionIdentitySyntaxAdmission().VisitApplication(parsed.Value!));
        var visitorError = Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(parsed.Value!));
        AssertCode(error, code);
        AssertCode(visitorError, code);
    }

    [Fact]
    public void should_preserve_the_authored_location()
    {
        var location = SourceLocation.Start with { Path = "Observer.play", Line = 7 };
        var error = Catch.Exception(() => new ReactionIdentitySyntaxAdmission().VisitNode(new ObserverFilterSyntax("Account", null, location)));
        ((UnsupportedObserverFilters)error).Location.ShouldEqual(location);
    }

    [Fact]
    public void should_admit_an_unmarked_unfiltered_reaction() => Catch.Exception(() => new ReactionIdentitySyntaxAdmission().VisitNode(new ReactionSyntax("Follow", [], SourceLocation.Start))).ShouldBeNull();

    static void AssertCode(Exception error, string code)
    {
        if (code == "STAGE-ESM-034") error.ShouldBeOfExactType<UnsupportedReactionIdentity>();
        else error.ShouldBeOfExactType<UnsupportedObserverFilters>();
        error.Message.StartsWith(code + ":", StringComparison.Ordinal).ShouldBeTrue();
    }
}
