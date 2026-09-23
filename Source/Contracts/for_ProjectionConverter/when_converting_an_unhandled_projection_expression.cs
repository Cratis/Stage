// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter;

public class when_converting_an_unhandled_projection_expression : Specification
{
    Exception? _error;

    void Because()
    {
        var location = SourceLocation.Start;
        var from = new FromSyntax(
            [new EventSpecSyntax("ItemRegistered", null, location)],
            null,
            null,
            [new SetMappingSyntax("name", new EnvironmentExpressionSyntax("NAME", location), location)],
            location);
        var projection = new ProjectionSyntax("Items", "Items", null, AutoMapMode.Inherit, null, [from], location);
        _error = Catch.Exception(() => ProjectionConverter.Convert(projection));
    }

    [Fact] void should_reject_the_expression_instead_of_storing_a_different_token() =>
        _error.ShouldBeOfExactType<UnsupportedProjectionConversion>();
}
