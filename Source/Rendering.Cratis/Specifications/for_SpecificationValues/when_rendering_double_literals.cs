// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Specifications.for_SpecificationValues;

public class when_rendering_double_literals : Specification
{
    readonly List<string> _diagnostics = [];
    string _decimal = null!;
    string _integer = null!;
    string _fraction = null!;

    void Because()
    {
        _decimal = Render(1.23d, "Decimal");
        _integer = Render(1.0000000000000002d, "Int");
        _fraction = Render(1.23d, "Int");
    }

    [Fact] void should_render_a_decimal_without_binary_residue() => _decimal.ShouldEqual("1.23m");
    [Fact] void should_use_legacy_rounding_before_testing_integrality() => _integer.ShouldEqual("1");
    [Fact] void should_reject_a_fraction_for_an_integer() => _fraction.ShouldEqual("default!");
    [Fact] void should_report_only_the_fractional_integer() => _diagnostics.Count.ShouldEqual(1);

    string Render(double value, string type) => SpecificationValues.Literal(value, new TypeRefSyntax(type, false, false, new SourceLocation(1, 1)), "value", "command", new ApplicationSet([]), _diagnostics);
}
