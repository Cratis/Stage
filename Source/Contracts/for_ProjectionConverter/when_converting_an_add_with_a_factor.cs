// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter;

public class when_converting_an_add_with_a_factor : Specification
{
    ProjectionSyntax _projection = null!;
    Exception? _error;

    void Establish()
    {
        var location = SourceLocation.Start;
        var mapping = new AddMappingSyntax("total", new PathExpressionSyntax("unitPrice", location), location)
        {
            Factor = new PathExpressionSyntax("quantity", location)
        };
        var from = new FromSyntax([new EventSpecSyntax("LineAdded", null, location)], null, null, [mapping], location);
        _projection = new ProjectionSyntax("Basket", "Basket", null, AutoMapMode.Inherit, null, [from], location);
    }

    void Because() => _error = Catch.Exception(() => ProjectionConverter.Convert(_projection));

    [Fact] void should_refuse_instead_of_dropping_the_factor() => _error.ShouldBeOfExactType<UnsupportedProjectionConversion>();
    [Fact] void should_name_the_operation() => _error!.Message.ShouldContain("add");
    [Fact] void should_name_the_target() => _error!.Message.ShouldContain("total");
    [Fact] void should_name_the_factor() => _error!.Message.ShouldContain("factor 'quantity'");
}
