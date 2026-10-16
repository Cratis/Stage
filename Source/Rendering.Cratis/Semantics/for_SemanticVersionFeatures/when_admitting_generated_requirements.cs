// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_generated_requirements : Specification
{
    SemanticCommand _command = null!;
    SemanticId _generated;

    void Establish()
    {
        var model = invoice_model.Compile("""
            concept Id : Uuid
            module M
              feature F
                slice StateChange S
                  command C
                    id Id generated identifier
                    count Int
                    validate
                      count min 1
                      require count > 0 and count < 10
                    returns id
            """);
        _command = SemanticVersionFeatures.Slices(model.Application).Single().Commands.Single();
        _generated = _command.Properties.Single(property => property.IsGenerated).Id;
    }

    [Fact] void should_admit_rules_and_requirements_over_request_values() => SemanticVersionFeatures.InCommand(_command).ShouldBeEmpty();
    [Fact] void should_refuse_a_rule_on_a_generated_property() => SemanticVersionFeatures.InCommand(_command with { Validations = [new(_generated, SemanticValidationRuleKind.NotEmpty, null, null)] }).Select(feature => feature.Code).ShouldContainOnly(SemanticVersionFeatures.CommandResults);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_refuse_generated_reads_in_either_operand_and_nested_conditions(bool nested, bool rightOperand)
    {
        var requirement = _command.Requirements.Single();
        var logical = (SemanticLogicalCondition)requirement.Condition;
        var comparison = (SemanticComparison)logical.Right;
        comparison = rightOperand ? comparison with { Right = new(_generated, null) } : comparison with { Left = new(_generated, null) };
        var condition = nested ? (SemanticCondition)(logical with { Right = comparison }) : comparison;
        SemanticVersionFeatures.InCommand(_command with { Requirements = [requirement with { Condition = condition }] }).Select(feature => feature.Code).ShouldContainOnly(SemanticVersionFeatures.CommandResults);
    }
}
