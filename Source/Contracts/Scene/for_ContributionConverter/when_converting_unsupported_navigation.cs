// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ContributionConverter;

public class when_converting_unsupported_navigation : Specification
{
    [Theory]
    [InlineData("Id")]
    [InlineData("Icon")]
    [InlineData("Presentation")]
    public void should_refuse_metadata_the_runtime_ignores(string member)
    {
        var contribution = new ContributionSyntax("Navigation", null, "Work items", 10, SourceLocation.Start);
        typeof(ContributionSyntax).GetProperty(member)!.SetValue(contribution, "authored");
        var error = Catch.Exception(() => ContributionConverter.Convert(contribution, "navigation"));
        error.ShouldBeOfExactType<UnsupportedUiSyntax>();
        ((UnsupportedUiSyntax)error).Member.ShouldEqual($"ContributionSyntax.{member}");
        error.Message.ShouldContain("The pinned Scene runtime does not honor this field.");
    }

    [Theory]
    [InlineData(ContributionDestinationKind.Outlet)]
    [InlineData(ContributionDestinationKind.Dialog)]
    [InlineData(ContributionDestinationKind.External)]
    public void should_refuse_every_unhonored_destination_kind(ContributionDestinationKind kind)
    {
        var contribution = new ContributionSyntax("Navigation", null, "Work items", 10, SourceLocation.Start)
        {
            Destination = new(kind, "target", SourceLocation.Start)
        };
        var error = Catch.Exception(() => ContributionConverter.Convert(contribution, "navigation"));
        error.ShouldBeOfExactType<UnsupportedUiSyntax>();
        ((UnsupportedUiSyntax)error).Member.ShouldEqual("ContributionSyntax.Destination");
        error.Message.ShouldContain(UnsupportedUiSyntax.DiagnosticCode);
        error.Message.ShouldContain("The pinned Scene runtime does not honor this field.");
    }
}
