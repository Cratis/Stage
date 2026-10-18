// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter.when_converting_with_diagnostics;

public class and_several_constructs_are_unsupported : Specification
{
    readonly ConversionDiagnostics _diagnostics = new();
    ProjectionDefinition? _result;

    void Because()
    {
        var location = SourceLocation.Start;
        var nested = new NestedSyntax(
            "details",
            AutoMapMode.Inherit,
            [new EverySyntax([new SetMappingSyntax("name", new EnvironmentExpressionSyntax("NAME", location), location)], false, (AutoMapMode)99, location)],
            location);
        _result = ProjectionConverter.Convert(new("Items", "Items", "audit", AutoMapMode.Inherit, null, [nested], location), _diagnostics, "Listing");
    }

    [Fact] void should_omit_the_whole_projection() => _result.ShouldBeNull();
    [Fact] void should_continue_scanning_nested_blocks_after_the_first_error() => _diagnostics.All.Select(diagnostic => diagnostic.Code).ToArray().ShouldEqual(["STAGE-PROJECTION-001", "STAGE-PROJECTION-007", "STAGE-PROJECTION-005"]);
}
