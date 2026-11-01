// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder;

public class when_isolating_locally_unadmitted_projections : given.a_locally_unadmitted_projection
{
    SemanticExecutionPlan _replay = null!;

    void Because() => _replay = SemanticProjectionAdmission.ForReplay(_plan);

    [Fact] void should_remove_only_the_refused_projection() => Assert.Equal(
        _plan.Projections.Values.Where(projection => projection.Name != "RefusedProjection").Select(projection => projection.Id).OrderBy(id => id.ToString()),
        _replay.Projections.Keys.OrderBy(id => id.ToString()));
    [Fact] void should_keep_all_event_contracts() => Assert.True(_plan.Events.Keys.ToHashSet().SetEquals(_replay.Events.Keys));
    [Fact] void should_keep_all_commands() => Assert.True(_plan.Commands.Keys.ToHashSet().SetEquals(_replay.Commands.Keys));
    [Fact] void should_keep_all_read_models() => Assert.True(_plan.ReadModels.Keys.ToHashSet().SetEquals(_replay.ReadModels.Keys));
    [Fact] void should_keep_all_constraints() => Assert.True(_plan.Constraints.Keys.ToHashSet().SetEquals(_replay.Constraints.Keys));
    [Fact] void should_keep_all_reactions() => Assert.Equal(_plan.Reactions.AsEnumerable(), _replay.Reactions.AsEnumerable());
    [Fact] void should_not_mutate_the_original_plan() => Assert.Equal(2, _plan.Projections.Count);
    [Fact] void should_not_erase_the_original_refusal() => Assert.NotNull(SemanticPlanAdmission.Check(_plan, _command.Id));
    [Fact] void should_reuse_the_isolated_plan() => Assert.Same(_replay, SemanticProjectionAdmission.ForReplay(_plan));
}
