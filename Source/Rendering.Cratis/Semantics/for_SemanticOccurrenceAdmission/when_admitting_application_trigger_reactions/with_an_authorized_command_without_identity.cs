// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_an_authorized_command_without_identity : given.an_application_trigger_reaction
{
    [Theory]
    [InlineData("      command FinishBatch\n        authorize CanFinish", "  feature Processing")]
    [InlineData("      command FinishBatch", "  feature Processing\n    authorize CanFinish")]
    public void should_refuse_explicit_and_inherited_authorization(string command, string feature)
    {
        InspectSource("policy CanFinish\n  require authenticated\n" + InvocationSource
            .Replace("      command FinishBatch", command, StringComparison.Ordinal)
            .Replace("  feature Processing", feature, StringComparison.Ordinal));
        Plan();
        _plan.Success.ShouldBeFalse();
        _plan.Diagnostics.Single(item => item.Code == "STAGE-ESM-024" && item.Artifact == _reaction.Id).Message.ShouldEqual(
            "Reaction 'ProcessBatch' invokes authorized command 'FinishBatch' from application trigger 'BatchRequested' without 'runs as'; occurrence commands always run as system (Cratis/Stage#343).");
    }
}
