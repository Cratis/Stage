// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_newer_minor_with_semantic_identities : given.a_source_plan
{
    ArtifactRenderPlan Plan(bool strict)
    {
        var model = _loaded.Model;
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion(32, 1));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion(32, 1));
        var projects = model.Application.Modules.Single(module => module.Name == "Projects");
        var request = new ArtifactRenderRequest(
            model,
            _loaded.Plan,
            CratisRendering.CreateProfile(_planOptions.ApplicationName, new(_planOptions.ProjectName, _planOptions.RootNamespace) { Strict = strict }),
            new(ArtifactRenderScopeKind.Feature, projects.Features.Single(feature => feature.Name == "Registration").Id))
        {
            SemanticIdentities = _loaded.SemanticIdentities,
            IdentityCatalog = _loaded.IdentityCatalog,
            ImplementationRequirements = _loaded.ImplementationRequirements,
            ImplementationContents = _loaded.ImplementationContents,
            TypedContextDescriptors = _loaded.TypedContextDescriptors,
            AttachmentDiagnostics = _loaded.AttachmentDiagnostics
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }

    [Fact]
    void should_keep_the_range_evidence_through_report_finalization_by_default()
    {
        var plan = Plan(strict: false);
        plan.Success.ShouldBeTrue();
        plan.PlanEvidence.Single().Diagnostic.Code.ShouldEqual(ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange);
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
        ReportOutcome(plan).ShouldEqual("Degraded");
    }

    [Fact]
    void should_refuse_strictly_through_report_finalization()
    {
        var plan = Plan(strict: true);
        plan.Success.ShouldBeFalse();
        plan.PlanEvidence.Single().Diagnostic.Code.ShouldEqual(ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange);
        plan.Diagnostics.Single(diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange).Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
        ReportOutcome(plan).ShouldEqual("Degraded");
    }

    static string ReportOutcome(ArtifactRenderPlan plan)
    {
        var report = plan.Artifacts.Single(artifact => artifact.RelativePath == ".stage/completeness.json");
        using var json = JsonDocument.Parse(report.Bytes.ToArray());
        json.RootElement.GetProperty("schemaVersion").GetString().ShouldEqual("stage-render-completeness/v2");
        return json.RootElement.GetProperty("outcome").GetString()!;
    }
}
