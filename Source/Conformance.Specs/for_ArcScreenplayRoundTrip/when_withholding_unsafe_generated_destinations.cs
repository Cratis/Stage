// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Screenplay;
using Cratis.Specifications;
using Microsoft.CodeAnalysis;
using Xunit;
using ScreenplaySeverity = Cratis.Screenplay.Diagnostics.DiagnosticSeverity;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_withholding_unsafe_generated_destinations : Specification
{
    [Theory]
    [InlineData("(string, EventForEventSourceId, AuthorAudited)", "(Name, WRAPPER, new AuthorAudited(Name))", "", "")]
    [InlineData("(string, object[])", "(Name, [WRAPPER, new AuthorAudited(Name)])", "", "")]
    [InlineData("Task<(string, EventForEventSourceId, AuthorAudited)>", "Task.FromResult((Name, WRAPPER, new AuthorAudited(Name)))", "", "")]
    [InlineData("(string, object[])", "(Name, [.. Build(), WRAPPER])", "public object[] Build() => [];", "")]
    [InlineData("(string, object[])", "(Name, [Build(), WRAPPER])", "public AuthorAudited Build() => new(Name);", "")]
    [InlineData("(string, EventForEventSourceId, EventForEventSourceId)", "(Name, WRAPPER, new EventForEventSourceId(auditId, new AuthorAudited(Name)))", "", ", [GeneratedValue(\"audit\", Freshness = GeneratedValueFreshness.Unguarded)] AuthorId auditId")]
    public void should_withhold_all_productions_and_require_a_handler_attachment(string signature, string expression, string helper, string additionalParameter)
    {
        var source = given.native_command_sources.Generated(signature, expression, helper, additionalParameter, "[Cratis.Chronicle.Keys.Key] AuthorId Id, ");
        var recovered = given.arc_recovery.Native(source);
        Assert.DoesNotContain(recovered.Compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Single(recovered.Result.Diagnostics, diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination);
        Assert.True(recovered.Syntax.Success, string.Join(Environment.NewLine, recovered.Syntax.Diagnostics));

        // Withholding must happen before binding; a native handler without an attachment is not executable.
        Assert.Empty(recovered.Command.Produces);
        Assert.DoesNotContain("produces ", recovered.Result.Source, StringComparison.Ordinal);
        Assert.False(recovered.Bound.Success);
        var refusal = Assert.Single(recovered.Bound.Diagnostics, diagnostic => diagnostic.Severity != ScreenplaySeverity.Information);
        Assert.Equal("PLAY0800", refusal.Code);
        Assert.Equal("handler", recovered.Result.Source.Split('\n')[refusal.Location.Line - 1].Trim());
        Assert.Equal(9, refusal.Location.Column);
        Assert.Equal("RoundTrip.play", refusal.Location.Path);
        Assert.DoesNotContain(" generated identifier", recovered.Result.Source, StringComparison.Ordinal);
        Assert.DoesNotContain("for author", recovered.Result.Source, StringComparison.Ordinal);
        Assert.DoesNotContain("for audit", recovered.Result.Source, StringComparison.Ordinal);
        Assert.DoesNotContain("for id", recovered.Result.Source, StringComparison.Ordinal);
    }
}
#endif
