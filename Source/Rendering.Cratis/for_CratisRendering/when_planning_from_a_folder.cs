// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_from_a_folder : given.a_source_plan
{
    async Task Because() => _result = await From(".");
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_exclude_unselected_slices() => Paths(_result).Any(path => path.StartsWith("Tasks/", StringComparison.Ordinal)).ShouldBeFalse();
}
