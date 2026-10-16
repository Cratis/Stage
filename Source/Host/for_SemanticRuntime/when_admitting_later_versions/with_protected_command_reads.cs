// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_protected_command_reads : Specification
{
    SemanticRuntimeAdmission _admission = null!;

    void Because()
    {
        var model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());
        _admission = new(SemanticExecutionPlan.Compile(model, []).Plan!);
    }

    [Fact] void should_admit_the_application_boundary() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_admit_every_protected_read_command() => _admission.Entries.ShouldNotContain(entry => entry.Kind == "command" && entry.Status != "supported");
    [Fact] void should_admit_the_version() => _admission.Blocking.ShouldNotContain(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal));
}
