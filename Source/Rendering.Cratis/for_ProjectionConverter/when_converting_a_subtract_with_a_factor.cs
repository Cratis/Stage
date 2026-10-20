// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Types;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ProjectionConverter;

public class when_converting_a_subtract_with_a_factor : Specification
{
    readonly List<string> _diagnostics = [];
    SubtractMappingSyntax _mapping = null!;
    ApplicationSet _applications = null!;
    MappedProperty _result = null!;

    void Establish()
    {
        var location = SourceLocation.Start;
        _mapping = new SubtractMappingSyntax("total", new PathExpressionSyntax("unitPrice", location), location)
        {
            Factor = new PathExpressionSyntax("quantity", location)
        };
        _applications = new ApplicationSet([]);
    }

    void Because() => _result = ProjectionMapping.Resolve(_mapping, "LineAdded", EventPropertyIndex.Build(_applications), _applications, _diagnostics);

    [Fact] void should_not_emit_a_factor_less_attribute() => _result.Attribute.ShouldBeNull();
    [Fact] void should_not_read_only_the_first_operand() => _result.SourcePath.ShouldBeNull();
    [Fact] void should_report_the_unsupported_product() => _diagnostics.Count.ShouldEqual(1);
    [Fact] void should_name_the_factor() => _diagnostics.Single().ShouldContain("factor 'Quantity'");
}
