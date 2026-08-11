// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_command_authorization : given.a_compiled_model_using_2x_constructs
{
    AuthorizationRequirement _authorization = null!;

    void Because() => _authorization = _slice.Command!.Authorization!;

    [Fact] void should_carry_the_requirement() => _authorization.ShouldNotBeNull();

    // 'IsAccountant or IsFinance and OwnsInvoice' — 'and' binds tighter, so the root is the 'or'. A flat list of
    // the three names could not tell this apart from '(IsAccountant or IsFinance) and OwnsInvoice'.
    [Fact] void should_root_the_tree_at_the_or() =>
        ((LogicalRequirement)_authorization).Operator.ShouldEqual(ProducedEventLogicalOperator.Or);
    [Fact] void should_put_the_first_policy_on_the_left() =>
        ((PolicyReference)((LogicalRequirement)_authorization).Left).Policy.ShouldEqual("IsAccountant");
    [Fact] void should_group_the_remaining_two_under_an_and() =>
        ((LogicalRequirement)((LogicalRequirement)_authorization).Right).Operator.ShouldEqual(ProducedEventLogicalOperator.And);
    [Fact] void should_name_every_policy_in_declaration_order() =>
        _authorization.Policies().ShouldContainOnly(["IsAccountant", "IsFinance", "OwnsInvoice"]);
}
