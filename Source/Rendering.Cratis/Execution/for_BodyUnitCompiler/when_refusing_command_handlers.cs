// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler;

/// <summary>Stage#891 S5: the worker refuses a handler exactly as render admission does, with the same code and message, and never emits.</summary>
public class when_refusing_command_handlers : Specification
{
    [Theory]
    [InlineData("return new OrderPlaced(context.Identity.Id);", "csharp", "STAGE-ESM-015", false)]
    [InlineData("return new object[] { 42 };", "csharp", "STAGE-ESM-019", false)]
    [InlineData("return [42]", "python", "STAGE-IMPL-002", false)]
    [InlineData(handler_model.Body, "csharp", "STAGE-IMPL-002", true)]
    void should_refuse_with_the_render_admission_code_and_message_without_emitting(string body, string language, string code, bool pending)
    {
        var request = handler_model.Request(body);
        var requirement = request.ImplementationRequirements.Single() with
        {
            Language = language,
            AttachmentResolution = pending ? SemanticAttachmentResolution.Pending : SemanticAttachmentResolution.Resolved
        };
        request = request with { ImplementationRequirements = [requirement] };
        var loaded = new LoadedSemanticModel(request.Model, request.ExecutionPlan)
        {
            ImplementationRequirements = request.ImplementationRequirements,
            TypedContextDescriptors = request.TypedContextDescriptors,
            ImplementationContents = request.ImplementationContents
        };
        var emissions = 0;
        var entry = BodyUnitCompiler.Compile(loaded, Path.Combine(AppContext.BaseDirectory, "body-unit-cache", Guid.NewGuid().ToString("N")), () => emissions++).Entries.Single();
        entry.Admitted.ShouldBeFalse();
        entry.DiagnosticCode.ShouldEqual(code);
        emissions.ShouldEqual(0);
        var rendered = handler_model.Plan(request with { Profile = CratisRendering.CreateProfile("HandlerModel", new("HandlerModel", "HandlerModel")) }).Diagnostics.First(diagnostic => diagnostic.Code == code && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        entry.DiagnosticMessage.ShouldEqual(BodyDiagnostics.Format(code, rendered.Message));
    }
}
#endif
