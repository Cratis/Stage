// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Guards;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_guarded_action_has_nested_conditions : given.a_guarded_action
{
    GuardCondition _condition = null!;

    void Establish()
    {
        var open = new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("draft", _location), _location);
        var many = new ComparisonConditionSyntax("item.count", ComparisonOperator.GreaterThan, new LiteralExpressionSyntax(3, _location), _location);
        var condition = new LogicalConditionSyntax(open, LogicalOperator.And, new LogicalConditionSyntax(open, LogicalOperator.Or, many, _location), _location);
        _action = new("Process invoice", [new ScreenActionAlternativeSyntax(condition, "SendInvoice", _location)], _location);
    }

    void Because()
    {
        var result = (ExternalComponent)ScreenDirectiveConverter.Convert([_action], "InvoiceDetails")[0];
        _condition = GuardedActionProperties.Read(result).Alternatives[0].Condition;
    }

    [Fact] void should_keep_the_outer_and() => ((LogicalGuardCondition)_condition).Operator.ShouldEqual(LogicalGuardOperator.And);
    [Fact] void should_keep_the_inner_or() => ((LogicalGuardCondition)((LogicalGuardCondition)_condition).Right).Operator.ShouldEqual(LogicalGuardOperator.Or);
    [Fact] void should_preserve_the_qualified_subject() => ((ComparisonGuardCondition)((LogicalGuardCondition)_condition).Left).Left.Subject.ShouldEqual("item.status");
    [Fact] void should_admit_the_nested_condition() => SceneGuards.CanEvaluate(_condition).ShouldBeTrue();
}
