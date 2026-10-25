// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter;

public class when_converting_a_subtract_with_a_factor : Specification
{
    ProjectionSyntax _projection = null!;
    Stage.Contracts.Projections.ProjectionDefinition _result = null!;

    void Establish()
    {
        var location = SourceLocation.Start;
        var mapping = new SubtractMappingSyntax("total", new PathExpressionSyntax("unitPrice", location), location)
        {
            Factor = new PathExpressionSyntax("quantity", location)
        };
        var from = new FromSyntax([new EventSpecSyntax("LineAdded", null, location)], null, null, [mapping], location);
        _projection = new ProjectionSyntax("Basket", "Basket", null, AutoMapMode.Inherit, null, [from], location);
    }

    void Because() => _result = ProjectionConverter.Convert(_projection);

    Stage.Contracts.Projections.PropertyMapping Mapping => _result.From["LineAdded"].Properties.Single();

    [Fact] void should_keep_the_first_operand() => Mapping.Expression.ShouldEqual("$subtract(unitPrice)");
    [Fact] void should_keep_the_factor() => Mapping.Factor.ShouldEqual("quantity");
    [Fact] void should_name_the_target() => Mapping.Property.ShouldEqual("total");
}
