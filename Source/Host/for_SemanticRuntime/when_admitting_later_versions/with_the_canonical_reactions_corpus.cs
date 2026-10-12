// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_the_canonical_reactions_corpus : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ReactionsCorpus.V6.EsmBytes.AsSpan())).Plan!;
    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_every_reaction() => _admission.Blocking.Where(entry => entry.Kind == "reaction").Select(entry => entry.Artifact).Distinct().Count().ShouldEqual(4);
    [Fact] void should_keep_the_reaction_code() => _admission.Blocking.Where(entry => entry.Kind == "reaction").All(entry => entry.Details!.StartsWith("STAGE-ESM-024:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_event_reaction_refused() => _admission.Blocking.Select(entry => entry.Details).ShouldContain("STAGE-ESM-024: Reaction 'ReminderScheduler' is an ESM v6 automation construct; the Stage Host and specification executor do not run reactions yet.");
    [Fact] void should_name_the_application_trigger_kind() => _admission.Blocking.Select(entry => entry.Details).ShouldContain("STAGE-ESM-024: Reaction 'PaymentImporter' uses trigger kind 'ApplicationTrigger', which Stage does not support yet.");
    [Fact] void should_name_the_schedule_kind() => _admission.Blocking.Select(entry => entry.Details).ShouldContain("STAGE-ESM-024: Reaction 'WeeklyDigest' uses trigger kind 'Schedule', which Stage does not support yet.");
    [Fact] void should_name_the_opaque_body() => _admission.Blocking.Select(entry => entry.Details).ShouldContain("STAGE-ESM-024: Reaction 'StartupBody' has an opaque body for trigger kind 'Startup', which Stage does not support yet.");
    [Fact] void should_name_the_startup_kind() => _admission.Blocking.Select(entry => entry.Details).ShouldContain("STAGE-ESM-024: Reaction 'StartupBody' uses trigger kind 'Startup', which Stage does not support yet.");
}
