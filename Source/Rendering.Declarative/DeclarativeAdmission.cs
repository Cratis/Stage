// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Kits;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents the outcome of admitting a render profile: a prepared kit bound to one language, or the refusal diagnostics.
/// </summary>
/// <param name="Kit">The loaded kit, when admitted.</param>
/// <param name="Language">The bound language, when admitted.</param>
/// <param name="Templates">The prepared templates, when admitted.</param>
/// <param name="Components">The components to record in the plan provenance.</param>
/// <param name="Diagnostics">The refusal diagnostics; empty when admitted.</param>
sealed record DeclarativeAdmission(
    LoadedKit? Kit,
    IDeclarativeLanguage? Language,
    PreparedTemplates? Templates,
    ImmutableArray<ArtifactRenderComponent> Components,
    ImmutableArray<ArtifactRenderDiagnostic> Diagnostics)
{
    internal const string KitInputName = "kit";

    internal bool Admitted => Diagnostics.IsEmpty;

    internal static DeclarativeAdmission Admit(ArtifactRenderProfile profile, ImmutableArray<IDeclarativeLanguage> languages, DeclarativePlannerOptions options)
    {
        if (profile.Renderer != DeclarativeArtifactRenderPlanner.Renderer || profile.RendererVersion != DeclarativeArtifactRenderPlanner.RendererVersion)
        {
            return Refuse(DeclarativeCodes.RendererMismatch, $"The profile renderer must be {DeclarativeArtifactRenderPlanner.Renderer} {DeclarativeArtifactRenderPlanner.RendererVersion}.");
        }

        if (profile.Inputs.Length != 1 || profile.Inputs[0].Name != KitInputName)
        {
            return Refuse(DeclarativeCodes.InputRoster, "The profile must carry exactly one input named kit.");
        }

        var input = profile.Inputs[0];
        var capabilities = new KitEngineCapabilities(0, [.. languages.Select(_ => _.Pack).Distinct()], [TemplateProfile.Contract, new("model", 1, 0)]);
        var loaded = KitLoader.Load(KitArchive.Read(input.Bytes), capabilities, options.KitLimits);
        if (!loaded.Succeeded)
        {
            return new(null, null, null, [], [.. loaded.Diagnostics.Select(_ => Error(_.Code, $"{_.Message} ({_.Path})"))]);
        }

        return Bind(profile, input, loaded.Kit!, languages);
    }

    static DeclarativeAdmission Bind(ArtifactRenderProfile profile, ArtifactRenderInput input, LoadedKit kit, ImmutableArray<IDeclarativeLanguage> languages)
    {
        if (kit.Digest != input.Sha256)
        {
            return Refuse(DeclarativeCodes.KitDigestMismatch, "The kit bytes do not match the recorded input digest.");
        }

        if (profile.Target != kit.Manifest.Target.Id || profile.TargetVersion != kit.Manifest.Target.Version)
        {
            return Refuse(DeclarativeCodes.TargetMismatch, "The profile target does not match the kit target.");
        }

        if (kit.Manifest.LanguagePacks.Length != 1)
        {
            return Refuse(DeclarativeCodes.LanguagePackCount, "The kit must bind exactly one language pack.");
        }

        var pack = kit.Manifest.LanguagePacks[0];
        var matches = languages.Where(_ => _.Pack == pack).ToArray();
        if (matches.Length != 1)
        {
            return Refuse(DeclarativeCodes.LanguageNotBound, $"The language pack {pack} must map to exactly one registered language.");
        }

        var prepared = TemplateProfile.Prepare(kit, matches[0]);
        return prepared.Templates is null
            ? new(kit, null, null, [], [.. prepared.Diagnostics.Select(_ => Error(_.Code, $"{_.Message} ({_.Path}:{_.Line})"))])
            : new(kit, matches[0], prepared.Templates, DeclarativeComponents.For(kit, pack), []);
    }

    static DeclarativeAdmission Refuse(string code, string message) => new(null, null, null, [], [Error(code, message)]);

    static ArtifactRenderDiagnostic Error(string code, string message) => new(code, ArtifactRenderDiagnosticSeverity.Error, message, default);
}
