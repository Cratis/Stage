// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_selecting_an_unknown_slice : given.a_source_plan
{
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([.. _featureSelection.Entries, PlanSelectionEntry.Slice("Projects", "Registration", "Unknown")]), _planOptions);
    [Fact] void should_refuse_the_missing_slice() => ShouldRefuse("STAGE-PLAN-013");
    [Fact] void should_not_render_the_known_portion() => _result.Artifacts.ShouldBeEmpty();
}
