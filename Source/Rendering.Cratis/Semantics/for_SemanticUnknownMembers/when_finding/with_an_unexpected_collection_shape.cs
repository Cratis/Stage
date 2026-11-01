// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_an_unexpected_collection_shape : a_populated_model
{
    void Establish() => typeof(SemanticApplication).GetProperty(nameof(SemanticApplication.Triggers))!
        .SetValue(_model.Application, default(ImmutableArray<SemanticApplicationTrigger>));

    void Because() => _unknown = SemanticUnknownMembers.Find(_model, _known);

    [Fact] void should_report_the_unreadable_member_without_throwing() => _unknown.Single().Member.ShouldEqual("SemanticApplication.Triggers");
    [Fact] void should_fail_closed() => _unknown.Single().SecurityRelevant.ShouldBeTrue();
}
