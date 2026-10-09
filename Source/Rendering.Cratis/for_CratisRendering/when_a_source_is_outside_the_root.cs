// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_a_source_is_outside_the_root : given.a_source_plan
{
    async Task Because() => _result = await From("../outside.play");
    [Fact] void should_refuse_the_source() => ShouldRefuse("STAGE-PLAN-001");
    [Fact] void should_not_publish_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
