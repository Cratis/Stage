// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Comparison;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_structural_value_comparisons : a_command_only_plan
{
    bool _numbersEqual;
    bool _arraysEqual;
    bool _arraysDifferent;
    bool _compositesEqual;
    bool _compositesDifferent;

    void Because()
    {
        var id = _specification.ThenEvents[0].Values[0].TargetProperty;
        _numbersEqual = SemanticExpectationComparer.AreEqual(SemanticValue.Number(1.5m), SemanticValue.Number(1.50m));
        _arraysEqual = SemanticExpectationComparer.AreEqual(SemanticValue.Array([SemanticValue.Number(1.5m)]), SemanticValue.Array([SemanticValue.Number(1.50m)]));
        _arraysDifferent = SemanticExpectationComparer.AreEqual(SemanticValue.Array([SemanticValue.Text("a")]), SemanticValue.Array([SemanticValue.Text("b")]));
        _compositesEqual = SemanticExpectationComparer.AreEqual(SemanticValue.Composite([new(id, SemanticValue.Number(1.5m))]), SemanticValue.Composite([new(id, SemanticValue.Number(1.50m))]));
        _compositesDifferent = SemanticExpectationComparer.AreEqual(SemanticValue.Composite([new(id, SemanticValue.Text("a"))]), SemanticValue.Composite([new(id, SemanticValue.Text("b"))]));
    }

    [Fact] void should_ignore_decimal_scale() => _numbersEqual.ShouldBeTrue();
    [Fact] void should_compare_arrays_recursively() => Xunit.Assert.True(_arraysEqual && !_arraysDifferent);
    [Fact] void should_compare_composites_recursively() => Xunit.Assert.True(_compositesEqual && !_compositesDifferent);
}
