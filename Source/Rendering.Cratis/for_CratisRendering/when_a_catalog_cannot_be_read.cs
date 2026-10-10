// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_a_catalog_cannot_be_read : given.a_source_plan
{
    async Task Because() => _result = await CratisRendering.PlanFrom(new PlaySources(_root, ["projects.play"], "absent-catalog.json"), _featureSelection, _planOptions);
    [Fact] void should_report_the_read_failure() => ShouldRefuse("STAGE-PLAN-005");
    [Fact] void should_preserve_the_catalog_source() => _result.Diagnostics.Single().Source.ShouldEqual("absent-catalog.json");
}
