// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_reaction_identity : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ReactionIdentityCorpus.V10.EsmBytes.AsSpan())).Plan!;
    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_reaction_identity() => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-034:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_admit_the_version_pair() => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal)).ShouldBeFalse();
}
