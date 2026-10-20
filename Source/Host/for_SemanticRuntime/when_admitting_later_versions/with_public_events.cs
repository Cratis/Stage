// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_public_events : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan())).Plan!;
    void Because() => _admission = new(_plan);

    [Fact] void should_not_block_local_behavior() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_explain_local_only_publication() => _admission.Entries.Where(entry => entry.Details?.StartsWith("STAGE-ESM-032:", StringComparison.Ordinal) == true).All(entry => entry.Status == "renderedAround" && entry.Details!.Contains("Published locally only", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explain_seed_only_foreign_events() => _admission.Entries.Where(entry => entry.Details?.StartsWith("STAGE-ESM-033:", StringComparison.Ordinal) == true).All(entry => entry.Status == "renderedAround" && entry.Details!.Contains("seeds", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_publication_entries() => _admission.Entries.Count(entry => entry.Details?.StartsWith("STAGE-ESM-032:", StringComparison.Ordinal) == true).ShouldEqual(2);
    [Fact] void should_report_foreign_event_entries() => _admission.Entries.Count(entry => entry.Details?.StartsWith("STAGE-ESM-033:", StringComparison.Ordinal) == true).ShouldEqual(2);
    [Fact] void should_report_both_translation_slices() => _admission.Entries.Count(entry => entry.Kind == "slice" && entry.Status == "renderedAround").ShouldEqual(2);
    [Fact] void should_report_the_public_projection() => _admission.Entries.Single(entry => entry.Artifact == _plan.Projections.Values.Single().Id.ToString()).Status.ShouldEqual("renderedAround");
    [Fact] void should_render_around_the_capture() => _admission.Entries.Where(entry => entry.Artifact == _plan.Captures.Values.Single().Id.ToString()).Select(entry => entry.Status).Distinct().ShouldContainOnly(["renderedAround"]);
    [Fact] void should_admit_the_version_pair() => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal)).ShouldBeFalse();
}
