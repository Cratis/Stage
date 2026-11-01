// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisStrictProfileInput
{
    internal const string InvalidInput = "The Cratis profile carries an invalid or duplicate strict acceptance input.";

    internal static ImmutableArray<ArtifactRenderInput> Create(bool strict) => strict ? [StrictAcceptanceInput.Create(true)] : [];

    internal static bool TryRead(ArtifactRenderProfile profile, out bool strict)
    {
        strict = false;
        var inputs = profile.Inputs.Where(input => input.Name == StrictAcceptanceInput.Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length != 1) return false;
        return StrictAcceptanceInput.TryRead(inputs[0], out strict);
    }

    internal static bool TryAdmit(ArtifactRenderProfile profile, CratisRenderingOptions options, [NotNullWhen(true)] out CratisRenderingOptions? admittedOptions, out string mismatch)
    {
        admittedOptions = null;
        mismatch = InvalidInput;
        if (!TryRead(profile, out var strict)) return false;
        admittedOptions = options with { Strict = strict };
        mismatch = string.Empty;
        return true;
    }

    internal static bool TryApply(ArtifactRenderProfile profile, bool strict, out ArtifactRenderProfile updated)
    {
        updated = profile;
        if (!strict) return true;
        if (!TryRead(profile, out _)) return false;
        updated = ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs.Where(input => input.Name != StrictAcceptanceInput.Name), StrictAcceptanceInput.Create(true)]);
        return true;
    }
}
