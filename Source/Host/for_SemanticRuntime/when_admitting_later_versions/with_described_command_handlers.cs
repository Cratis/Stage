// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_described_command_handlers : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan()), []).Plan!;
    void Because() => _admission = new(_plan);

    [Fact] void should_read_the_v23_vector() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
    [Fact] void should_admit_the_version() => _admission.Blocking.ShouldNotContain(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal));
    [Fact] void should_name_the_command_and_requirement() => _admission.Blocking.ShouldContain(entry => entry.Details!.StartsWith("STAGE-IMPL-002:", StringComparison.Ordinal) && entry.Details.Contains("ApplyLateFee", StringComparison.Ordinal) && entry.Details.Contains(_plan.Commands.Values.Single(command => command.Handler is not null).Handler!.RequirementId, StringComparison.Ordinal));
}
