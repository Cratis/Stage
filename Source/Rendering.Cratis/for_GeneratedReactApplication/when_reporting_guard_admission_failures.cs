// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

public class when_reporting_guard_admission_failures : given.a_screen_composition_render
{
    const string Payload = """
        {"screens":[{"properties":{"alternatives":[{"condition":{
          "kind":"logical","operator":"And",
          "left":{"kind":"comparison","operator":"Equal","left":{"subject":"item.status"},"right":"private-guard-literal"},
          "right":{"kind":"comparison","operator":"equal","left":{"subject":"item.status"},"right":"private-guard-literal"}
        }}]}}]}
        """;

    [Fact]
    void should_report_only_guards_rejected_by_the_production_predicate()
    {
        var rejected = RejectedGuards(JsonNode.Parse(Payload)).ToArray();
        rejected.ShouldEqual([
            "/screens/0/properties/alternatives/0/condition: kind=\"logical\", operator=\"And\"",
            "/screens/0/properties/alternatives/0/condition/right: kind=\"comparison\", operator=\"equal\""
        ]);
        string.Join('\n', rejected).ShouldNotContain("item.status");
        string.Join('\n', rejected).ShouldNotContain("private-guard-literal");
    }

    [Theory]
    [InlineData("STAGE-CRATIS-001")]
    [InlineData(UnsupportedGuardedScreenAction.DiagnosticCode)]
    void should_report_existing_diagnostics_before_artifact_extraction(string code)
    {
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options);
        var request = new ArtifactRenderRequest(_model, _executionPlan, profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        var diagnostic = new ArtifactRenderDiagnostic(code, ArtifactRenderDiagnosticSeverity.Error, "Admission refused.", _model.Application.Id);
        var plan = ArtifactRenderPlan.Create(request, [], [diagnostic]);
        var input = CratisArtifactRenderInput.CreateText(SceneCompositionInput.RelativePath, CratisRendering.TargetVersion, Payload);

        var failure = Assert.ThrowsAny<Exception>(() => RequireArtifacts(plan, [input]));

        failure.Message.ShouldContain($"{diagnostic.Code} | {diagnostic.Severity} | {diagnostic.Message} | {diagnostic.Artifact}");
        failure.Message.ShouldContain("guards rejected by SceneGuards admission (test-side re-evaluation)");
        failure.Message.ShouldContain("/screens/0/properties/alternatives/0/condition/right");
        failure.Message.ShouldNotContain("item.status");
        failure.Message.ShouldNotContain("private-guard-literal");
    }

    [Fact]
    void should_refuse_an_empty_plan_without_diagnostics_before_single_can_throw()
    {
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options);
        var request = new ArtifactRenderRequest(_model, _executionPlan, profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        var plan = ArtifactRenderPlan.Create(request, [], []);
        Assert.ThrowsAny<Exception>(() => Text(plan, "package.json")).Message.ShouldContain("Expected successful admission and generated artifacts.");
    }

    [Fact]
    void should_preserve_the_existing_diagnostic_when_a_payload_cannot_be_reparsed()
    {
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options);
        var request = new ArtifactRenderRequest(_model, _executionPlan, profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        var diagnostic = new ArtifactRenderDiagnostic("STAGE-CRATIS-001", ArtifactRenderDiagnosticSeverity.Error, "Malformed JSON.", _model.Application.Id);
        var plan = ArtifactRenderPlan.Create(request, [], [diagnostic]);
        var input = CratisArtifactRenderInput.CreateText(SceneCompositionInput.RelativePath, CratisRendering.TargetVersion, "private-guard-literal");
        var failure = Assert.ThrowsAny<Exception>(() => RequireArtifacts(plan, [input]));
        failure.Message.ShouldContain($"{diagnostic.Code} | {diagnostic.Severity} | {diagnostic.Message} | {diagnostic.Artifact}");
        failure.Message.ShouldContain("guard re-evaluation unavailable (");
        failure.Message.ShouldNotContain("private-guard-literal");
    }

    [Fact]
    void should_report_legacy_envelopes_using_the_production_binding_predicate()
    {
        var payload = JsonNode.Parse("""{"bindings":[{"trigger":{},"condition":{"path":"$guard","parameters":{"subject":"private-guard-literal"}}}]}""");
        RejectedGuards(payload).ShouldEqual(["/bindings/0/condition: kind=<absent>, operator=<absent>"]);
    }
}
