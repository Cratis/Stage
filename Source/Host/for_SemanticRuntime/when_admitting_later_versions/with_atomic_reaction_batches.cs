// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_atomic_reaction_batches : Specification
{
    SemanticRuntimeAdmission _admission = null!;

    void Because()
    {
        var model = SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(LanguageVersion.V23, SemanticVersion.V23, model.Application with { ReactionProductionMode = SemanticReactionProductionMode.Atomic }), []).Plan!;
        _admission = new(plan);
    }

    [Fact] void should_admit_the_application_boundary() => _admission.Blocking.ShouldNotContain(entry => entry.Details!.StartsWith("STAGE-ESM-043:", StringComparison.Ordinal));
    [Fact] void should_admit_the_version() => _admission.Blocking.ShouldNotContain(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal));
}
