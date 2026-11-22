// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_carrying_semantic_exclusions;

public class and_replacing_plan_evidence : for_DeclarativeArtifactRenderPlanner.given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;
    string _report = string.Empty;

    void Because()
    {
        var files = KitFiles();
        files["templates/index.sbn"] = Bytes("{{ gap 'disabled_specification' 'Missing index' 'model.unknown' }}");
        var request = Request(Profile(Archive(files)));
        var exclusion = new SemanticExclusion(SemanticExclusionKind.Trigger, "RemovedTrigger", null, new(3, 1, "model.play"),
            [Diagnostic.Error("PLAY0053", "Unknown trigger.", new(3, 1, "model.play"))]);
        _plan = Planner().Plan(request with { Exclusions = [exclusion] });
        _report = Encoding.UTF8.GetString(ArtifactRenderReports.CreateUnsupported(_plan).Bytes.AsSpan());
    }

    [Fact] void should_keep_both_evidence_families() => _plan.PlanEvidence.Length.ShouldEqual(2);
    [Fact] void should_keep_the_typed_exclusion_after_inventory_and_provenance() => _plan.Exclusions.Single().Kind.ShouldEqual(SemanticExclusionKind.Trigger);
    [Fact] void should_own_the_exclusion_on_the_application() => _plan.PlanEvidence.Single(evidence => evidence.Diagnostic.Message.Contains("RemovedTrigger", StringComparison.Ordinal)).Diagnostic.Artifact.ShouldEqual(_plan.Scope.Artifact);
    [Fact] void should_keep_the_shared_report() => _report.Contains("Trigger: RemovedTrigger", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_fail_strict_acceptance_but_allow_publication() => (_plan.WithAcceptance(_plan.Degradations, strict: true).Success == false && _plan.WithAcceptance(_plan.Degradations, strict: true).CanPublishArtifacts).ShouldBeTrue();
}
