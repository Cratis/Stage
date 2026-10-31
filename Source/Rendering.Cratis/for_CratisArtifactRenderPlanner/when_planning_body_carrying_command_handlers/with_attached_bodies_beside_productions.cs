// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

/// <summary>
/// Stage#817 renders typed C# inline and file bodies beside productions using the compiler's handler descriptor.
/// The pinned compiler still needs a synthetic resolved attachment (Screenplay#1068); ambient access and
/// untyped returns remain refused, and foreign languages receive STAGE-IMPL-LANGUAGE even in Stub mode.
/// </summary>
public class with_attached_bodies_beside_productions
{
    internal const string TypedBody = "return new LateFeeApplied(context.Command.Days * 10);";

    const string Source = """
        module Billing
          feature Fees
            slice StateChange ApplyLateFee
              command ApplyLateFee
                invoiceId String identifier
                days Int
                handler
                  implementation
                    hint "Calculate the fee from overdue days"
                produces LateFeeApplied
                  for invoiceId
              event LateFeeApplied
                amount Int
        """;

    [Theory]
    [InlineData(null, ImplementationGapMode.Refuse)]
    [InlineData("Handlers/ApplyLateFee.cs", ImplementationGapMode.Refuse)]
    [InlineData(null, ImplementationGapMode.Stub)]
    [InlineData("Handlers/ApplyLateFee.cs", ImplementationGapMode.Stub)]
    public void should_render_typed_csharp_bodies_with_the_compiler_descriptor(string? file, ImplementationGapMode mode)
    {
        var request = Request(TypedBody, "csharp", file);
        var command = Handler(request.Model);
        var productions = command.Produces;
        var plan = Plan(request, mode);

        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.NotEqual(ArtifactRenderOutcome.Refused, plan.Outcome);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" || diagnostic.Code == "STAGE-ESM-020");
        Assert.Empty(plan.Refusals);
        Assert.NotEmpty(plan.Artifacts);
        Assert.Contains(plan.Artifacts, artifact => artifact.Sources.Contains(command.Id));
        Assert.Equal(productions, Handler(request.Model).Produces);
        var source = string.Join('\n', plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        Assert.Contains(TypedBody, source, StringComparison.Ordinal);
        Assert.Empty(plan.ImplementationGaps);
    }

    [Theory]
    [InlineData("return new LateFeeApplied(context.Identity.Id);", "csharp", null, "STAGE-ESM-015")]
    [InlineData("return new object[] { 42 };", "csharp", "Handlers/ApplyLateFee.cs", "STAGE-ESM-019")]
    [InlineData("return [42]", "python", null, "STAGE-IMPL-LANGUAGE")]
    [InlineData("return [42]", "python", "Handlers/apply_late_fee.py", "STAGE-IMPL-LANGUAGE")]
    public void should_refuse_unavailable_untyped_and_foreign_bodies(string body, string language, string? file, string code)
    {
        var request = Request(body, language, file);
        foreach (var mode in new[] { ImplementationGapMode.Refuse, ImplementationGapMode.Stub })
        {
            var plan = Plan(request, mode);
            var command = Handler(request.Model);
            Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Artifact == command.Id);
            Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
            Assert.Empty(plan.Artifacts);
            Assert.Empty(plan.ImplementationGaps);
            if (language != "csharp")
            {
                Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == code &&
                    diagnostic.Message.Contains(language, StringComparison.Ordinal) &&
                    diagnostic.Message.Contains(command.Handler!.RequirementId, StringComparison.Ordinal) &&
                    diagnostic.Message.Contains("Add a csharp body to render it with the Cratis target.", StringComparison.Ordinal));
            }
        }
    }

    internal static ArtifactRenderRequest Request(string body = TypedBody, string language = "csharp", string? file = null)
    {
        var request = handler_model.Request(body, Source);
        var command = Handler(request.Model);
        var descriptor = Assert.Single(request.TypedContextDescriptors);
        Assert.True(descriptor.IsWrapperReady);
        Assert.Equal(request.Model.Revision, descriptor.ModelRevision);
        Assert.Equal(
            command.Properties.Select(property => new SemanticContextProperty(property.Name, property.Id, property.Type)),
            descriptor.Members.Single(member => member.Name == "Command").Type.Properties);
        var requirement = request.ImplementationRequirements.Single() with { Language = language, File = file };
        return request with { ImplementationRequirements = [requirement] };
    }

    static SemanticCommand Handler(ExecutableSemanticModel model) =>
        model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Commands).Single(command => command.Handler is not null);

    static ArtifactRenderPlan Plan(ArtifactRenderRequest request, ImplementationGapMode mode) =>
        CratisRendering.Plan(request.Model, request.ExecutionPlan, request.Scope, new("Described", "Described") { ImplementationGaps = mode }, request.ImplementationRequirements, request.ImplementationContents, [], request.TypedContextDescriptors);
}
