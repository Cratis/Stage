// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_an_enum_value_removed : a_populated_model
{
    void Because()
    {
        _known.Remove("SemanticSliceKind.StateChange");
        _unknown = SemanticUnknownMembers.Find(_model, _known);
    }

    [Fact] void should_report_the_zero_valued_enum() => _unknown.ShouldNotBeEmpty();
    [Fact] void should_report_only_the_removed_value() => _unknown.All(member => member.Member == "SemanticSliceKind.StateChange").ShouldBeTrue();
    [Fact] void should_use_the_enclosing_slice_owner() => _unknown[0].Owner.ShouldEqual(_model.Application.Modules[0].Features[0].Slices[0].Id);
    [Fact] void should_not_change_the_shared_ledger() => SemanticSurfaceLedger.Entries.ContainsKey($"{nameof(SemanticSliceKind)}.StateChange").ShouldBeTrue();
}
