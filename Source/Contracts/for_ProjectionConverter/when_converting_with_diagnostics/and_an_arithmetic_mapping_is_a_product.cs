// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter.when_converting_with_diagnostics;

// ESM v29 'add total by unitPrice * quantity': the definition carries the second operand, so nothing is lost and nothing is reported.
public class and_an_arithmetic_mapping_is_a_product : Specification
{
    [Theory]
    [InlineData("add")]
    [InlineData("subtract")]
    public void should_convert_the_product_without_a_diagnostic(string operation)
    {
        var location = new SourceLocation(12, 11, "basket.play");
        var value = new PathExpressionSyntax("unitPrice", location);
        var factor = new PathExpressionSyntax("quantity", location);
        MappingSyntax mapping = operation == "add"
            ? new AddMappingSyntax("total", value, location) { Factor = factor }
            : new SubtractMappingSyntax("total", value, location) { Factor = factor };
        var from = new FromSyntax([new EventSpecSyntax("LineAdded", null, location)], null, null, [mapping], location);
        var projection = new ProjectionSyntax("Basket", "Basket", null, AutoMapMode.Inherit, null, [from], location);
        var diagnostics = new ConversionDiagnostics();

        ProjectionConverter.Convert(projection, diagnostics, "Basket").ShouldNotBeNull();
        diagnostics.All.ShouldBeEmpty();
    }

    [Fact]
    public void should_still_convert_a_plain_add()
    {
        var location = new SourceLocation(12, 11, "basket.play");
        var from = new FromSyntax([new EventSpecSyntax("LineAdded", null, location)], null, null, [new AddMappingSyntax("total", new PathExpressionSyntax("amount", location), location)], location);
        var projection = new ProjectionSyntax("Basket", "Basket", null, AutoMapMode.Inherit, null, [from], location);
        var diagnostics = new ConversionDiagnostics();

        ProjectionConverter.Convert(projection, diagnostics, "Basket").ShouldNotBeNull();
        diagnostics.All.ShouldBeEmpty();
    }
}
