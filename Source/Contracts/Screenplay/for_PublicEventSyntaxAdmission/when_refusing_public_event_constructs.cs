// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_PublicEventSyntaxAdmission;

public class when_refusing_public_event_constructs : Specification
{
    [Theory]
    [InlineData("public event Published", "STAGE-ESM-032")]
    [InlineData("event Published from \"producer\"", "STAGE-ESM-033")]
    [InlineData("direction outbound", "STAGE-ESM-024")]
    [InlineData("direction inbound", "STAGE-ESM-024")]
    [InlineData("capture Import\n        source events\n          from Published", "STAGE-ESM-033")]
    public void should_refuse_before_legacy_translation(string construct, string code)
    {
        var source = "module Integration\n  feature Exchange\n    slice Translate Publish\n      " + construct;
        var parsed = new ScreenplayCompiler().Parse(source);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        var application = parsed.Value!;
        var error = Catch.Exception(() => new PublicEventSyntaxAdmission().VisitApplication(application));
        AssertCode(error, code);
        var visitorError = Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(application));
        AssertCode(visitorError, code);
    }

    [Fact]
    public void should_preserve_the_authored_location()
    {
        var location = SourceLocation.Start with { Path = "Publication.play", Line = 7 };
        var error = Catch.Exception(() => new PublicEventSyntaxAdmission().VisitNode(new EventSyntax("Published", [], location) { Visibility = EventVisibility.Public }));
        ((UnsupportedPublicEvents)error).Location.ShouldEqual(location);
    }

    [Fact]
    public void should_admit_private_local_events() => Catch.Exception(() => new PublicEventSyntaxAdmission().VisitNode(new EventSyntax("Registered", [], SourceLocation.Start))).ShouldBeNull();

    static void AssertCode(Exception error, string code)
    {
        switch (code)
        {
            case "STAGE-ESM-032": error.ShouldBeOfExactType<UnsupportedPublicEvents>(); break;
            case "STAGE-ESM-033": error.ShouldBeOfExactType<UnsupportedForeignEvents>(); break;
            default: error.ShouldBeOfExactType<UnsupportedTranslationDirection>(); break;
        }
        error.Message.StartsWith(code + ":", StringComparison.Ordinal).ShouldBeTrue();
    }
}
