// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticProductionSelection;

public class when_selecting_canonical_generated_productions : Specification
{
    readonly List<bool> _matches = [];

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(GeneratedValuesCorpus.V15.EsmBytes.AsSpan())).Plan!;
        foreach (var specification in plan.Specifications.Values.Where(specification => specification.ThenErrors.IsEmpty))
        {
            var reference = (SemanticAccepted)new SemanticSpecificationRunner().Run(plan, specification.Id).Execution;
            var taken = SemanticProductionSelection.Taken(plan.Model.Application, plan.Commands[specification.When!.Command], specification);
            _matches.Add(taken.Select(produced => produced.EventContract).SequenceEqual(reference.Facts.Select(fact => fact.EventContract)));
        }
    }

    [Fact] void should_match_the_reference_for_taken_and_skipped_productions() => _matches.ShouldContainOnly(true, true);
}
