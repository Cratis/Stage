// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_differing_override_stream_id : a_production_routed_plan
{
    async Task Because()
    {
        var expected = _specification.ThenEvents[1];
        var changed = _specification with { ThenEvents = [_specification.ThenEvents[0], expected with { Route = expected.Route! with { StreamId = SemanticValue.Text("archive-2") } }] };
        var model = _plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [changed] }] }] }] };
        await Run(SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!);
    }

    [Fact] void should_fail_like_the_reference() => AssertParity(false);
}
