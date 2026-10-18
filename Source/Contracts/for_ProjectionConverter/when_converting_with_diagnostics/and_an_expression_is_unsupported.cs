// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter.when_converting_with_diagnostics;

public class and_an_expression_is_unsupported : Specification
{
    [Theory]
    [InlineData("ListExpressionSyntax")]
    [InlineData("ObjectExpressionSyntax")]
    [InlineData("ContextExpressionSyntax")]
    [InlineData("IdentityExpressionSyntax")]
    [InlineData("EnvironmentExpressionSyntax")]
    [InlineData("StringsExpressionSyntax")]
    [InlineData("SourceItemExpressionSyntax")]
    public void should_name_the_expression_at_its_source_location(string kind)
    {
        var location = new SourceLocation(9, 13, "listing.play");
        ExpressionSyntax expression = kind switch
        {
            "ListExpressionSyntax" => new ListExpressionSyntax([], location),
            "ObjectExpressionSyntax" => new ObjectExpressionSyntax([], location),
            "ContextExpressionSyntax" => new ContextExpressionSyntax("tenant", location),
            "IdentityExpressionSyntax" => new IdentityExpressionSyntax("name", location),
            "EnvironmentExpressionSyntax" => new EnvironmentExpressionSyntax("NAME", location),
            "StringsExpressionSyntax" => new StringsExpressionSyntax("title", location),
            _ => new SourceItemExpressionSyntax("name", location)
        };
        var from = new FromSyntax([new EventSpecSyntax("Registered", null, location)], null, null, [new SetMappingSyntax("name", expression, location)], location);
        var projection = new ProjectionSyntax("Items", "Items", null, AutoMapMode.Inherit, null, [from], location);
        var diagnostics = new ConversionDiagnostics();

        ProjectionConverter.Convert(projection, diagnostics, "Listing").ShouldBeNull();
        diagnostics.All.Count.ShouldEqual(1);
        diagnostics.All[0].Code.ShouldEqual("STAGE-PROJECTION-005");
        diagnostics.All[0].Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostics.All[0].Location.ShouldEqual(location);
        diagnostics.All[0].Path.ShouldEqual("Listing");
        diagnostics.All[0].Construct.ShouldEqual($"projection expression {kind}");
    }
}
