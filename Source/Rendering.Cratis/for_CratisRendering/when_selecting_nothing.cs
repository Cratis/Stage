// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_selecting_nothing : given.a_source_plan
{
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([]), _planOptions);
    [Fact] void should_require_an_explicit_selection() => ShouldRefuse("STAGE-PLAN-010");
    [Fact] void should_not_render_anything() => _result.Artifacts.ShouldBeEmpty();
}
