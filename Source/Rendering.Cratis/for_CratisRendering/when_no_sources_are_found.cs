// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_no_sources_are_found : given.a_source_plan
{
    void Establish() => Directory.CreateDirectory(Path.Combine(_root, "empty"));
    async Task Because() => _result = await From("empty");
    [Fact] void should_report_an_empty_source_set() => ShouldRefuse("STAGE-PLAN-002");
    [Fact] void should_not_render_any_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
