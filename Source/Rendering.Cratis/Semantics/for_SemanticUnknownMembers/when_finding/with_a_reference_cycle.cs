// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_a_reference_cycle : a_populated_model
{
    void Establish()
    {
        AddAuthenticatedPolicy();
        var condition = new SemanticNotPolicyCondition(new SemanticAuthenticatedCondition());
        typeof(SemanticPolicy).GetProperty(nameof(SemanticPolicy.Condition))!.SetValue(_model.Application.Policies[0], condition);
        typeof(SemanticNotPolicyCondition).GetProperty(nameof(SemanticNotPolicyCondition.Operand))!.SetValue(condition, condition);
    }

    void Because() => _unknown = SemanticUnknownMembers.Find(_model, _known);

    [Fact] void should_terminate_without_spurious_diagnostics() => _unknown.ShouldBeEmpty();
}
