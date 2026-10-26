// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_the_audited_ledger : a_populated_model
{
    void Because() => _unknown = SemanticUnknownMembers.Find(_model, _known);

    [Fact] void should_have_no_false_positives() => _unknown.ShouldBeEmpty();
}
