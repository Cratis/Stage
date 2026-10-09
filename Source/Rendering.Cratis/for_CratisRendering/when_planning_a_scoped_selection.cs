// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_scoped_selection : given.a_source_plan
{
    CratisPlanResult _scaffold = null!;
    void Establish() => _scaffold = CratisRendering.PlanScaffold(_planOptions);
    void Because() => _result = CratisRendering.PlanFrom(_loaded, _featureSelection, _planOptions);
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_exclude_every_scaffold_artifact() => Paths(_result).Intersect(Paths(_scaffold), StringComparer.Ordinal).ShouldBeEmpty();
}
