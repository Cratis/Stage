// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_a_claim_reading_gate : given.an_application_trigger_reaction
{
    void Because()
    {
        InspectSource("policy CanFinish\n  require claim \"sub\" matches \"[System]\"\n" + InvocationSource
            .Replace("      command FinishBatch", "      command FinishBatch\n        authorize CanFinish", StringComparison.Ordinal)
            .Replace("      reaction ProcessBatch", "      reaction ProcessBatch\n        runs as system role \"Worker\"", StringComparison.Ordinal));
        Plan();
    }

    [Fact] void should_refuse_claim_reads() => _plan.Diagnostics.Select(item => item.Code).ShouldContain("STAGE-ESM-034");
    [Fact] void should_not_refuse_the_occurrence_identity_itself() => _diagnostics.ShouldBeEmpty();
}
