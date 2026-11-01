// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Carries what the executable model does not: the identity providers, the personas and the onboarding of the application, with the
/// files onboarding refers to. It travels in the immutable render profile, so it is part of the input digest like any other input.
/// </summary>
internal static class ApplicationRealizationInput
{
    internal const string Name = "cratis-realization:application";
    internal const string Version = "1";

    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, ApplicationRealization realization)
    {
        var inputs = profile.Inputs.Where(input => input.Name != Name).ToImmutableArray();

        return realization.IsEmpty && inputs.Length == profile.Inputs.Length ? profile : ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            realization.IsEmpty ? inputs : inputs.Add(ArtifactRenderInput.Create(Name, Version, [.. Serialize(realization)])));
    }

    internal static bool TryRead(ArtifactRenderProfile profile, out ApplicationRealization? realization)
    {
        realization = null;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0)
        {
            return true;
        }

        return inputs.Length == 1 && TryRead(inputs[0], out realization);
    }

    internal static ApplicationRealization? From(ArtifactRenderProfile profile) => TryRead(profile, out var realization) ? realization : null;

    static bool TryRead(ArtifactRenderInput input, out ApplicationRealization? realization)
    {
        realization = null;
        if (input.Version != Version || input.Sha256 != Convert.ToHexStringLower(SHA256.HashData(input.Bytes.AsSpan())))
        {
            return false;
        }

        try
        {
            var wire = JsonSerializer.Deserialize<Wire>(new UTF8Encoding(false, true).GetString(input.Bytes.AsSpan()), _options);
            if (wire is null)
            {
                return false;
            }

            var read = new ApplicationRealization(wire.Providers, wire.Personas, wire.Onboarding, wire.Environment)
            {
                Texts = wire.Texts.ToImmutableSortedDictionary(StringComparer.Ordinal),
                Images = wire.Images.ToImmutableSortedDictionary(image => image.Key, image => ImmutableArray.Create(Convert.FromBase64String(image.Value)), StringComparer.Ordinal)
            };

            // Only a canonical encoding is admitted, so the digest of the profile identifies the realization.
            if (!Serialize(read).SequenceEqual(input.Bytes))
            {
                return false;
            }

            realization = read;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    static byte[] Serialize(ApplicationRealization realization) =>
        JsonSerializer.SerializeToUtf8Bytes(
            new Wire(
                realization.Providers,
                realization.Personas,
                realization.Onboarding,
                realization.Environment,
                new SortedDictionary<string, string>(realization.Texts, StringComparer.Ordinal),
                new SortedDictionary<string, string>(realization.Images.ToDictionary(image => image.Key, image => Convert.ToBase64String([.. image.Value])), StringComparer.Ordinal)),
            _options);

    sealed record Wire(
        ImmutableArray<RealizedProvider> Providers,
        ImmutableArray<RealizedPersona> Personas,
        RealizedOnboarding? Onboarding,
        ImmutableArray<RealizedEnvironmentKey> Environment,
        SortedDictionary<string, string> Texts,
        SortedDictionary<string, string> Images);
}
