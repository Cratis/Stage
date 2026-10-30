// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    static (ArtifactRenderProfile? Profile, CratisPlanDiagnostic? Diagnostic) PrepareStrictProfile(CratisPlanOptions options, ArtifactRenderProfile profile, string domain)
    {
        if (!CratisStrictProfileInput.TryApply(profile, options.Strict, out var updated))
        {
            return (null, ProfileError(CratisStrictProfileInput.InvalidInput));
        }
        return PrepareProfileOptions(options, updated, domain);
    }

    static (ArtifactRenderProfile? Profile, CratisPlanDiagnostic? Diagnostic) PrepareProfileOptions(CratisPlanOptions options, ArtifactRenderProfile profile, string domain)
    {
        if (!Enum.IsDefined(options.Refusals))
        {
            return (null, ProfileError("The refusal mode is unknown."));
        }
        if (options.Refusals != ArtifactRefusalMode.Refuse)
        {
            if (profile.Inputs.Any(input => input.Name == RefusalModeInput.Name))
            {
                return (null, ProfileError("The caller's profile must omit refusal mode when the Refusals option is set."));
            }
            profile = ArtifactRenderProfile.Create(
                profile.Target,
                profile.TargetVersion,
                profile.Renderer,
                profile.RendererVersion,
                [.. profile.Inputs, RefusalModeInput.Create(options.Refusals)]);
        }
        return PrepareFileHeader(options, profile, domain);
    }

    static (ArtifactRenderProfile? Profile, CratisPlanDiagnostic? Diagnostic) PrepareFileHeader(CratisPlanOptions options, ArtifactRenderProfile profile, string domain)
    {
        if (!ArtifactFileHeader.IsValid(options.FileHeader))
        {
            return (null, CratisPlanResult.Error("STAGE-PLAN-023", "Every file header line must be non-null plain text without line breaks, control, private-use or bidirectional formatting characters."));
        }
        if (!options.FileHeader.IsDefaultOrEmpty)
        {
            if (profile.Inputs.Any(ArtifactFileHeader.IsInput))
            {
                return (null, ProfileError("The caller's profile must omit the file header when the FileHeader option is set."));
            }
            profile = FileHeaderInput.With(profile, options.FileHeader);
        }
        return (DomainPlacementInput.WithDomain(profile, domain), null);
    }
}
