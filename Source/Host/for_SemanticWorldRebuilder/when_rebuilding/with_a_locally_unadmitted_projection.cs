// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_locally_unadmitted_projection : given.a_locally_unadmitted_projection
{
    SemanticWorld _world = null!;
    Exception? _failure;

    void Because() => _failure = Record.Exception(() => _world = SemanticWorldRebuilder.Create(_plan, [_event], 0));

    [Fact] void should_establish_the_world() => Assert.Null(_failure);
    [Fact] void should_retain_the_stored_fact() => Assert.Single(_world.Facts);
    [Fact] void should_build_the_supported_projection() => Assert.Single(_world.ReadModels.Where(model => model.ReadModel == _readModel.Id));
    [Fact] void should_not_build_the_unadmitted_projection() => Assert.DoesNotContain(_world.ReadModels, model => model.ReadModel == _plan.ReadModels.Values.Single(model => model.Name == "RefusedSummary").Id);
    [Fact] void should_keep_the_original_admission_refusal() => Assert.NotNull(SemanticPlanAdmission.Check(_plan, _plan.ReadModels.Values.Single(model => model.Name == "RefusedSummary").Id));
}
