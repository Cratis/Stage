// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_a_command_invocation : given.an_application_trigger_reaction
{
    void Because()
    {
        const string command = "      command FinishBatch\n        batchId String identifier";
        InspectSource(Source.Replace("      reaction ProcessBatch", command + "\n      reaction ProcessBatch", StringComparison.Ordinal)
            .Replace("          produces BatchStarted\n            for batchId\n            batchId = batchId", "          invokes FinishBatch\n            batchId = batchId", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_a_command_invocation() =>
        _diagnostics.Single(item => item.Artifact == _reaction.Id && item.Code == "STAGE-ESM-024").Message.ShouldContain("invokes command 'FinishBatch'");
}
