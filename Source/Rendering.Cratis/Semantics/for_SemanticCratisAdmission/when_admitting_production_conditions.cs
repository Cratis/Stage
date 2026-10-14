// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_production_conditions : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    [Theory]
    [InlineData("expression")]
    [InlineData("text-ordering")]
    public void should_refuse_nonportable_conditions(string shape)
    {
        var located = _context.SelectedSlices().Single(slice => slice.Slice.Name == "Start");
        var command = located.Slice.Commands.Single();
        var produced = command.Produces.Single();
        var id = command.Properties.Single(property => property.Name == "id");
        produced = shape == "expression"
            ? produced with { Condition = SemanticExpression.FromValue(SemanticValue.Boolean(true)) }
            : produced with { When = new SemanticComparison(new(id.Id, null), SemanticComparisonOperator.GreaterThan, new(default, SemanticValue.Text("a"))) };
        var slice = located.Slice with { Commands = [command with { Produces = [produced] }] };
        var diagnostics = SemanticCratisAdmission.Evaluate(_context, [located with { Slice = slice }]);
        diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-006");
    }
}
