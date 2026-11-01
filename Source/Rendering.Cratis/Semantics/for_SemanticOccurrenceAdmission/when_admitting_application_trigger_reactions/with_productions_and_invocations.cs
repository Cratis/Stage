// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_productions_and_invocations : given.an_application_trigger_reaction
{
    internal static string MixedSource => InvocationSource.Replace("          invokes FinishBatch", "          produces BatchStarted\n            for batchId\n            batchId = batchId\n          invokes FinishBatch", StringComparison.Ordinal)
        + "\n          invokes FinishBatch\n            batchId = \"second\"\n";

    void Because()
    {
        InspectSource(MixedSource);
        Plan();
    }

    [Fact] void should_admit_mixed_effects() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_plan_multiple_invocations() => _plan.Success.ShouldBeTrue();
}
