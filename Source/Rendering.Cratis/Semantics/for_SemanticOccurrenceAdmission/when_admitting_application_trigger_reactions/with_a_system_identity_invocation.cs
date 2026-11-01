// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_a_system_identity_invocation : given.an_application_trigger_reaction
{
    void Because()
    {
        InspectSource("policy CanFinish\n  require role \"Worker\"\n" + InvocationSource
            .Replace("      command FinishBatch", "      command FinishBatch\n        authorize CanFinish", StringComparison.Ordinal)
            .Replace("      reaction ProcessBatch", "      reaction ProcessBatch\n        runs as system role \"Worker\"", StringComparison.Ordinal));
        Plan();
    }

    [Fact] void should_admit_the_execution_identity() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_render_the_authorized_command() => _plan.Success.ShouldBeTrue();
}
