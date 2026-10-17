// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_atomic_reaction_batches.given;

public class an_observed_reaction_plan : when_settling_reactions.given.a_reaction_plan
{
    protected int _worldFacts;

    protected async Task ExecuteObserved()
    {
        var specification = _plan.Specifications.Values.Single();
        _reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => _worldFacts = count };
        _result = (await executor.Run(_plan, new([specification.Id]), new())).Results.Single();
    }
}
