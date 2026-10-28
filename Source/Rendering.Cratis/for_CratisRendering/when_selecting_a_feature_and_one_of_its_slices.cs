// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_selecting_a_feature_and_one_of_its_slices : given.a_source_plan
{
    CratisPlanResult _featureOnly = null!;
    void Establish() => _featureOnly = CratisRendering.PlanFrom(_loaded, _featureSelection, _planOptions);
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([.. _featureSelection.Entries, .. _sliceSelection.Entries, .. _sliceSelection.Entries]), _planOptions);
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_union_overlapping_and_repeated_semantic_owners() => _result.Plan!.Inventory!.Digest.ShouldEqual(_featureOnly.Plan!.Inventory!.Digest);
    [Fact] void should_record_the_explicit_additional_scope_once() => _result.Plan!.AdditionalScopes.Length.ShouldEqual(1);
    [Fact] void should_hash_the_different_report_scope_provenance() => _result.Digest.ShouldNotEqual(_featureOnly.Digest);
}
