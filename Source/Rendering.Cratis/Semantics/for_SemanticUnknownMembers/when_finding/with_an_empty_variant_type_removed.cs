// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_an_empty_variant_type_removed : a_populated_model
{
    void Establish() => AddAuthenticatedPolicy();

    void Because()
    {
        _known.Remove("SemanticAuthenticatedCondition.$type");
        _unknown = SemanticUnknownMembers.Find(_model, _known);
    }

    [Fact] void should_report_the_empty_variant() => _unknown.Single().Member.ShouldEqual("SemanticAuthenticatedCondition.$type");
    [Fact] void should_fall_back_to_the_application_owner() => _unknown.Single().Owner.ShouldEqual(_model.Application.Id);
    [Fact] void should_retain_the_application_kind() => _unknown.Single().OwnerKind.ShouldEqual("application");
    [Fact] void should_not_expose_the_policy_name() => _unknown.Single().Path.ShouldEqual("Application.Policies[0].Condition");
}
