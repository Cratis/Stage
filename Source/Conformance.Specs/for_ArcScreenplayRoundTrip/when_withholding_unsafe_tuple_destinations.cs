// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Screenplay;
using Cratis.Specifications;
using Microsoft.CodeAnalysis;
using Xunit;
using ScreenplaySeverity = Cratis.Screenplay.Diagnostics.DiagnosticSeverity;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_withholding_unsafe_tuple_destinations : Specification
{
    [Theory]
    [InlineData("public (string, EventForEventSourceId) Handle() { var wrapped = WRAPPER; return (Name, wrapped); }")]
    [InlineData("public (string, object[]) Handle() => (Name, [.. Build(), WRAPPER]); public object[] Build() => [];")]
    [InlineData("public (string, object[]) Handle() => (Name, [Build(), WRAPPER]); public AuthorAudited Build() => new(Name);")]
    [InlineData("public (string, (EventForEventSourceId, AuthorAudited)) Handle() => (Name, (WRAPPER, new(Name)));")]
    [InlineData("public (string, string, EventForEventSourceId) Handle() => (Name, Month, WRAPPER);")]
    [InlineData("public (Receipt, EventForEventSourceId) Handle() => (Converted, WRAPPER); public ConvertedReceipt Converted => new(Name);")]
    public void should_withhold_the_entire_unsafe_tuple_including_its_safe_sibling(string handler)
    {
        // Never weaken this to assert only that the unsafe sibling was dropped: that changes the command's meaning.
        var recovered = given.arc_recovery.Native(given.native_command_sources.Tuple(handler));
        recovered.AssertHealthy(false, ScreenplayDiagnosticCodes.UnrepresentableProductionDestination);
        Assert.Empty(recovered.BoundCommand.Produces);
        Assert.Null(recovered.BoundCommand.Response);
        Assert.Null(recovered.BoundCommand.Route);
        Assert.DoesNotContain(recovered.BoundCommand.Properties, property => property.IsGenerated);
        Assert.DoesNotContain("for id", recovered.Result.Source, StringComparison.Ordinal);
        Assert.DoesNotContain("for otherId", recovered.Result.Source, StringComparison.Ordinal);
        Assert.DoesNotContain("for thirdId", recovered.Result.Source, StringComparison.Ordinal);
        Assert.DoesNotContain(" generated identifier", recovered.Result.Source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId + \"computed\", new AuthorAudited(Name)));")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Subject = \"someone\" });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Tags = [\"tag\"] });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { Occurred = DateTimeOffset.UtcNow });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { EventStreamId = Month + \"computed\" });")]
    [InlineData("public (string, EventForEventSourceId, EventForEventSourceId) Handle() => (Name, WRAPPER, new(OtherId, new AuthorAudited(Name)) { EventSource = typeof(LedgerEventSource), EventStream = \"Unknown\" });")]
    public void should_withhold_all_productions_and_require_a_handler_attachment(string handler)
    {
        var recovered = given.arc_recovery.Native(given.native_command_sources.Tuple(handler));
        Assert.DoesNotContain(recovered.Compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Single(recovered.Result.Diagnostics, diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination);
        Assert.Empty(recovered.Command.Produces);
        Assert.DoesNotContain("produces ", recovered.Result.Source, StringComparison.Ordinal);
        Assert.True(recovered.Syntax.Success, string.Join(Environment.NewLine, recovered.Syntax.Diagnostics));
        Assert.False(recovered.Bound.Success);
        var refusal = Assert.Single(recovered.Bound.Diagnostics, diagnostic => diagnostic.Severity != ScreenplaySeverity.Information);
        Assert.Equal(ScreenplaySeverity.Error, refusal.Severity);
        Assert.Equal("PLAY0800", refusal.Code);
        Assert.Equal("Command 'RegisterAuthor' handler requires a constrained implementation attachment.", refusal.Message);
        Assert.Equal("handler", recovered.Result.Source.Split('\n')[refusal.Location.Line - 1].Trim());
        Assert.Equal(20, refusal.Location.Line);
        Assert.Equal(9, refusal.Location.Column);
        Assert.Equal("RoundTrip.play", refusal.Location.Path);
    }

    // Keep the fixture opaque; inlining it would test a different recovery boundary.
    [Fact]
    public void should_not_discover_a_destination_inside_a_helper_method()
    {
        var recovered = given.arc_recovery.Native(given.native_command_sources.Tuple("public (string, EventForEventSourceId) Handle() => (Name, Build()); public EventForEventSourceId Build() => AUDIT;"));
        Assert.DoesNotContain(recovered.Compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Empty(recovered.Result.Diagnostics);
        Assert.True(recovered.Syntax.Success, string.Join(Environment.NewLine, recovered.Syntax.Diagnostics));
        Assert.False(recovered.Bound.Success);
        Assert.Equal(["PLAY0800"], recovered.Bound.Diagnostics.Where(diagnostic => diagnostic.Severity != global::Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code));
        Assert.Empty(recovered.Command.Produces);
        Assert.DoesNotContain("for thirdId", recovered.Result.Source, StringComparison.Ordinal);
    }
}
#endif
