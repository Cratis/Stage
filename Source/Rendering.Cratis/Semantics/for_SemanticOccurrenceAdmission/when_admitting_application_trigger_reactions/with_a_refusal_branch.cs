// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_a_refusal_branch : given.an_application_trigger_reaction
{
    void Because() => InspectSource(InvocationSource + "\n            on refused by validation\n              produces BatchStarted\n                for batchId\n                batchId = batchId\n");

    [Fact] void should_refuse_the_unrecoverable_branch() => _diagnostics.Single(item => item.Code == "STAGE-ESM-024" && item.Artifact == _reaction.Id).Message.ShouldEqual(
        "Reaction 'ProcessBatch' handles refusals of 'FinishBatch' from application trigger 'BatchRequested'; occurrence receipts do not preserve the refusal kind (Cratis/Stage#343).");
}
