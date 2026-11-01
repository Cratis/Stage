// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_an_undefined_enum_value : a_populated_model
{
    void Establish() => typeof(SemanticSlice).GetProperty(nameof(SemanticSlice.Kind))!
        .SetValue(_model.Application.Modules[0].Features[0].Slices[0], (SemanticSliceKind)9876);

    void Because() => _unknown = SemanticUnknownMembers.Find(_model, _known);

    [Fact] void should_report_a_structural_key_without_the_raw_value() => _unknown.Single().Member.ShouldEqual("SemanticSliceKind.$value");
    [Fact] void should_keep_the_slice_owner() => _unknown.Single().Owner.ShouldEqual(_model.Application.Modules[0].Features[0].Slices[0].Id);
    [Fact] void should_fail_closed() => _unknown.Single().SecurityRelevant.ShouldBeTrue();
}
