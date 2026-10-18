// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_checking_uuid_equality : Specification
{
    const string Granted = "a1111111-1111-4111-8111-111111111111";
    const string Other = "b2222222-2222-4222-8222-222222222222";

    static readonly SemanticId _member = SemanticId.Create(SemanticKind.Property, "member");
    static readonly SemanticId _requestedBy = SemanticId.Create(SemanticKind.Property, "requestedBy");

    [Theory]
    [InlineData(Granted, Granted, true)]
    [InlineData(Granted, Other, false)]
    public void should_compare_two_properties_ordinally(string left, string right, bool equal)
    {
        var values = new Dictionary<SemanticId, SemanticValue> { [_member] = SemanticValue.Text(left), [_requestedBy] = SemanticValue.Text(right) };

        Check(SemanticComparisonOperator.Equal, new(_member, null), new(_requestedBy, null), values).ShouldEqual(equal);
        Check(SemanticComparisonOperator.NotEqual, new(_member, null), new(_requestedBy, null), values).ShouldEqual(!equal);
    }

    [Theory]
    [InlineData(Granted, true)]
    [InlineData(Other, false)]
    public void should_compare_a_property_with_a_literal(string value, bool equal)
    {
        var values = new Dictionary<SemanticId, SemanticValue> { [_member] = SemanticValue.Text(value) };
        var literal = new SemanticConditionOperand(default, SemanticValue.Text(Granted));

        Check(SemanticComparisonOperator.Equal, new(_member, null), literal, values).ShouldEqual(equal);
        Check(SemanticComparisonOperator.NotEqual, new(_member, null), literal, values).ShouldEqual(!equal);
    }

    [Theory]
    [InlineData(Granted, true)]
    [InlineData(Other, false)]
    public void should_compare_a_read_path_with_a_property(string requestedBy, bool equal)
    {
        var values = new Dictionary<SemanticId, SemanticValue> { [_requestedBy] = SemanticValue.Text(requestedBy) };
        var reads = new Dictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>
        {
            ["PlantAccessGrant"] = new Dictionary<SemanticId, SemanticValue> { [_member] = SemanticValue.Text(Granted) }
        };
        var grantedMember = new SemanticConditionOperand(_member, null) { Read = "PlantAccessGrant" };

        Check(SemanticComparisonOperator.Equal, grantedMember, new(_requestedBy, null), values, reads).ShouldEqual(equal);
        Check(SemanticComparisonOperator.NotEqual, grantedMember, new(_requestedBy, null), values, reads).ShouldEqual(!equal);
    }

    static bool Check(
        SemanticComparisonOperator comparison,
        SemanticConditionOperand left,
        SemanticConditionOperand right,
        IReadOnlyDictionary<SemanticId, SemanticValue> values,
        IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads = null) =>
        SemanticRuleEvaluation.Condition(new SemanticComparison(left, comparison, right), values, reads);
}
