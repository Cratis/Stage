// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Policies;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

/// <summary>
/// A command's authorization carried the policy names it requires and nothing said what any of those names
/// checks, so a consumer could report that a command is guarded and never that it is guarded by a role.
/// </summary>
public class when_inspecting_the_policies : given.a_compiled_model_using_previously_dropped_constructs
{
    [Fact] void should_carry_every_declared_policy() =>
        _model.Policies.Select(policy => policy.Name)
            .ShouldContainOnly(["IsAuthenticated", "IsAccountant", "IsFinanceDepartment", "OwnsInvoice", "CanWriteOff", "IsAdultCustomer"]);

    [Fact] void should_carry_an_authenticated_condition() =>
        Policy("IsAuthenticated").Condition.ShouldBeOfExactType<AuthenticatedCondition>();
    [Fact] void should_carry_the_required_role() =>
        ((RoleCondition)Policy("IsAccountant").Condition!).Role.ShouldEqual("Accountant");

    [Fact] void should_carry_the_claim_a_condition_names() =>
        Claim("IsFinanceDepartment").Claim.ShouldEqual("department");
    [Fact] void should_not_match_a_valued_claim_against_the_subject() =>
        Claim("IsFinanceDepartment").MatchesSubject.ShouldBeFalse();
    [Fact] void should_carry_the_value_a_claim_is_matched_against() =>
        Claim("IsFinanceDepartment").ValueKind.ShouldEqual(ProducedValueKind.Literal);
    [Fact] void should_match_a_subject_claim_against_the_subject() =>
        Claim("OwnsInvoice").MatchesSubject.ShouldBeTrue();
    [Fact] void should_state_no_value_for_a_subject_claim() =>
        Claim("OwnsInvoice").Value.ShouldBeEmpty();

    [Fact] void should_keep_the_shape_of_a_combined_condition() =>
        Logical(Policy("CanWriteOff").Condition!).Operator.ShouldEqual(ProducedEventLogicalOperator.And);
    [Fact] void should_keep_a_parenthesized_group_on_the_side_it_was_written() =>
        Logical(Logical(Policy("CanWriteOff").Condition!).Left).Operator.ShouldEqual(ProducedEventLogicalOperator.Or);
    [Fact] void should_keep_the_leaves_of_a_combined_condition() =>
        Logical(Policy("CanWriteOff").Condition!).Right.ShouldBeOfExactType<AuthenticatedCondition>();

    [Fact] void should_state_no_condition_for_a_policy_implemented_in_code() =>
        Policy("IsAdultCustomer").Condition.ShouldBeNull();
    [Fact] void should_name_the_language_a_policy_is_implemented_in() =>
        Policy("IsAdultCustomer").CodeLanguage.ShouldEqual("csharp");
    [Fact] void should_name_no_language_for_a_declarative_policy() =>
        Policy("IsAccountant").CodeLanguage.ShouldBeEmpty();

    [Fact] void should_derive_identifiers_deterministically() =>
        EventModelLoader.LoadFromSource(Source).Policies[0].Id.ShouldEqual(_model.Policies[0].Id);

    PolicyDefinition Policy(string name) => _model.Policies.Single(policy => policy.Name == name);

    ClaimCondition Claim(string name) => (ClaimCondition)Policy(name).Condition!;

    static LogicalPolicyCondition Logical(PolicyCondition condition) => (LogicalPolicyCondition)condition;
}
