// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_supplying_context_only_files : given.a_source_plan
{
    async Task Because() => _result = await From("projects.play", "context.play", "tasks.play");
    [Fact] void should_compile_all_supplied_sources() => _loaded.Model.Application.Modules.Length.ShouldEqual(2);
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_exclude_unused_concepts() => Paths(_result).ShouldNotContain("Common/Unused.cs");
    [Fact] void should_exclude_unselected_modules() => Paths(_result).Any(path => path.StartsWith("Tasks/", StringComparison.Ordinal)).ShouldBeFalse();
}
