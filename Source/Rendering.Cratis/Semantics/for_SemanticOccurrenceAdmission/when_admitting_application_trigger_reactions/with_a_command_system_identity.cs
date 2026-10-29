// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_a_command_system_identity : given.an_application_trigger_reaction
{
    void Because() => InspectSource(Source.Replace("      reaction ProcessBatch", "      reaction ProcessBatch\n        runs as system role \"Worker\"", StringComparison.Ordinal));

    [Fact]
    void should_keep_the_targeted_identity_refusal_after_generic_admission()
    {
        var diagnostic = _diagnostics.Single(item => item.Artifact == _reaction.Id);
        diagnostic.Code.ShouldEqual("STAGE-ESM-024");
        diagnostic.Message.ShouldContain("'runs as'");
        diagnostic.Message.ShouldContain("ProcessBatch");
        diagnostic.Message.ShouldContain("BatchRequested");
    }
}
