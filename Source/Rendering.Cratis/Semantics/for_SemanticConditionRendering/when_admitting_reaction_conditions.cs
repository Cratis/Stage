// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticConditionRendering;

public class when_admitting_reaction_conditions : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    [Theory]
    [InlineData("optional")]
    [InlineData("collection")]
    [InlineData("composite")]
    [InlineData("text-ordering")]
    public void should_refuse_nonportable_operands(string form)
    {
        var property = _context.Events.Values.Single(@event => @event.Name == "Started").Properties.Single(property => property.Name == "amount");
        property = property with
        {
            Type = form switch
            {
                "optional" => property.Type with { IsOptional = true },
                "collection" => property.Type with { IsCollection = true },
                "composite" => property.Type with { Kind = SemanticTypeReferenceKind.CompositeType },
                _ => property.Type with { Primitive = SemanticPrimitiveType.Text }
            }
        };
        var condition = new SemanticComparison(new(property.Id, null), SemanticComparisonOperator.GreaterThan, new(default, SemanticValue.Number(0)));
        SemanticConditionRendering.CanRender(condition, [property], _context, numericOrderingOnly: true).ShouldBeFalse();
    }

    [Fact]
    public void should_refuse_an_enum_valued_reaction_guard_without_artifacts()
    {
        SetContext(Compile(("concept Rate : Enum\n  low\n  high\n" + Source).Replace("amount Decimal", "amount Rate", StringComparison.Ordinal)
            .Replace("where amount > 0", "where amount == low", StringComparison.Ordinal)));
        Plan();
        _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-039");
        _plan.Artifacts.ShouldBeEmpty();
    }
}
