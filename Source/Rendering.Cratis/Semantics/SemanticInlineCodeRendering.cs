// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal sealed record SemanticInlineCodeSite(string RequirementId, SemanticId Owner, InlineCodePlacement Placement);

internal sealed record SemanticInlineCodeResult(InlineCode? Code, InlineCodePlacement Placement, ImmutableArray<ArtifactRenderDiagnostic> Diagnostics)
{
    internal bool Refused => Code is null;
    internal bool Degraded => Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-DEGRADE-003");
    internal string? EffectiveInputFingerprint => Code is null ? null : CSharpInlineLanguagePack.Fingerprint([Code]);
}

// Kept independent of #807's not-yet-delivered acceptance/evidence types.
internal static class SemanticInlineCodeRendering
{
    internal static SemanticInlineCodeResult Generation(SemanticConcept concept, SemanticApplicationContext context)
    {
        var placement = new InlineCodePlacement("generation", concept.Id.ToString(), SemanticGenerationRendering.BodyMethod(concept));
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var site = new SemanticInlineCodeSite(concept.Generation!.RequirementId!, concept.Id, placement);
        if (!SemanticImplementationAdmission.TryGetVerifiedInline(context.Request, site, diagnostics, out var code))
        {
            return new(null, placement, diagnostics.ToImmutable());
        }

        var requirement = context.Request.ImplementationRequirements.Single(item => item.RequirementId == code!.RequirementId);
        if (!GenerationEnvelope(requirement))
        {
            diagnostics.Add(new("STAGE-INLINE-INTEGRITY", ArtifactRenderDiagnosticSeverity.Error, "The generation envelope has an invalid role, version or capability.", concept.Id));
            return new(null, placement, diagnostics.ToImmutable());
        }

        var presentation = CSharpInlineLanguagePack.Present(code!, verbatim: true);
        foreach (var diagnostic in presentation.Diagnostics)
        {
            var severity = presentation.Degraded ? ArtifactRenderDiagnosticSeverity.Warning : ArtifactRenderDiagnosticSeverity.Error;
            diagnostics.Add(new(diagnostic.Code, severity, diagnostic.Message, concept.Id));
        }

        return new(presentation.Text is null ? null : code, placement, diagnostics.ToImmutable());
    }

    internal static SemanticInlineCodeResult Reaction(LocatedSemanticSlice located, SemanticReaction reaction, SemanticApplicationContext context) =>
        SemanticReactionBodyRendering.Inline(located, reaction, context);

    internal static SemanticInlineCodeResult CommandBody(SemanticCommand command, SemanticApplicationContext context)
    {
        var placement = new InlineCodePlacement("command", command.Id.ToString(), SemanticCommandHandlerRendering.Method(command));
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var site = new SemanticInlineCodeSite(command.Handler!.RequirementId, command.Id, placement);
        if (!SemanticImplementationAdmission.TryGetVerifiedInline(context.Request, site, diagnostics, out var code))
        {
            return new(null, placement, diagnostics.ToImmutable());
        }
        var requirement = context.Request.ImplementationRequirements.Single(item => item.RequirementId == code!.RequirementId);
        if (!CommandEnvelope(requirement))
        {
            diagnostics.Add(new("STAGE-ESM-019", ArtifactRenderDiagnosticSeverity.Error, "The command handler envelope has an invalid role, version or capability.", command.Id));
            return new(null, placement, diagnostics.ToImmutable());
        }
        if (CarriageReturnLiterals.Find(code!.Text) is { } literal)
        {
            // PlannedArtifact.CreateText publishes LF; a CR inside a literal would silently change its value.
            diagnostics.Add(new("STAGE-ESM-022", ArtifactRenderDiagnosticSeverity.Error, $"Carriage return in {literal} of command '{command.Name}' handler body changes emitted text.", command.Id));
            return new(null, placement, diagnostics.ToImmutable());
        }
        var presentation = CSharpInlineLanguagePack.Present(code!, verbatim: true);
        if (presentation.Degraded)
        {
            diagnostics.Add(new("STAGE-INLINE-INTEGRITY", ArtifactRenderDiagnosticSeverity.Error, "The command handler body has no supported containment proof; a placeholder is not a command payload.", command.Id));
            return new(null, placement, diagnostics.ToImmutable());
        }
        foreach (var diagnostic in presentation.Diagnostics)
        {
            diagnostics.Add(new(diagnostic.Code, presentation.Degraded ? ArtifactRenderDiagnosticSeverity.Warning : ArtifactRenderDiagnosticSeverity.Error, diagnostic.Message, command.Id));
        }
        return new(presentation.Text is null ? null : code, placement, diagnostics.ToImmutable());
    }

    static bool CommandEnvelope(SemanticImplementationRequirement requirement) =>
        requirement.Role == SemanticImplementationRole.CommandHandler && requirement.ContextVersion == 1 && requirement.ResultVersion == 1 &&
        string.Equals(requirement.RequiredCapability, "provider-defined", StringComparison.Ordinal);

    static bool GenerationEnvelope(SemanticImplementationRequirement requirement) =>
        requirement.Role == SemanticImplementationRole.ValueGeneration && requirement.ContextVersion == 1 &&
        requirement.ResultVersion == 1 && string.Equals(requirement.RequiredCapability, "provider-defined", StringComparison.Ordinal);
}
