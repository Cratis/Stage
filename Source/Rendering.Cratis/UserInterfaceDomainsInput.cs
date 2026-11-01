// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines the hashed v1 user interface domains input: the slices whose generated frontend proxies are present under
/// the application's other domain folders. It accompanies the <see cref="UserInterfaceInput"/> so the shared UI imports
/// each of those proxies from the domain folder it was generated under; its absence places every present slice under
/// the plan's own domain.
/// </summary>
public static class UserInterfaceDomainsInput
{
    /// <summary>
    /// The stable profile input name.
    /// </summary>
    public const string Name = "cratis-render:user-interface-domains";

    /// <summary>
    /// The profile input format version.
    /// </summary>
    public const string Version = "1";

    /// <summary>
    /// Creates the canonical JSON object of normalized domain sub-paths, in ordinal order, each holding its distinct
    /// slice identities in ordinal order.
    /// </summary>
    /// <param name="domains">The slice identities by domain sub-path.</param>
    /// <returns>The hashed profile input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">A domain is invalid or repeated, or an identity is blank or present under two domains.</exception>
    public static ArtifactRenderInput Create(IEnumerable<KeyValuePair<string, IEnumerable<string>>> domains)
    {
        ArgumentNullException.ThrowIfNull(domains);
        var canonical = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (domain, slices) in domains)
        {
            if (domain is null || !DomainPlacementInput.TryNormalize(domain, out var normalized))
            {
                throw new InvalidArtifactRenderContract($"A user interface domain '{domain}' must contain valid relative C# name segments, without traversal or empty segments.");
            }

            if (!canonical.TryAdd(normalized, [.. (slices ?? []).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]))
            {
                throw new InvalidArtifactRenderContract($"The user interface domain '{normalized}' is named more than once.");
            }
        }

        var identities = canonical.Values.SelectMany(slices => slices).ToArray();
        if (identities.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidArtifactRenderContract("A user interface domains input requires non-blank slice identities.");
        }

        if (identities.GroupBy(identity => identity, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1) is { } repeated)
        {
            throw new InvalidArtifactRenderContract($"The slice '{repeated.Key}' is present under more than one user interface domain.");
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var (domain, slices) in canonical)
            {
                writer.WriteStartArray(domain);
                foreach (var slice in slices)
                {
                    writer.WriteStringValue(slice);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        return ArtifactRenderInput.Create(Name, Version, [.. stream.ToArray()]);
    }

    /// <summary>
    /// Reads the slice identities by domain carried by a profile.
    /// </summary>
    /// <param name="profile">The render profile.</param>
    /// <param name="domains">The slice identities by normalized domain sub-path, or null when the profile carries no such input.</param>
    /// <returns>Whether the profile carries at most one canonical user interface domains input.</returns>
    public static bool TryRead(ArtifactRenderProfile profile, out ImmutableDictionary<string, ImmutableArray<string>>? domains)
    {
        ArgumentNullException.ThrowIfNull(profile);
        domains = null;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length > 1 || inputs[0].Version != Version) return false;

        try
        {
            var read = JsonSerializer.Deserialize<Dictionary<string, string[]>>(new UTF8Encoding(false, true).GetString(inputs[0].Bytes.AsSpan()));
            if (read?.Values.Any(slices => slices is null) != false) return false;
            if (!Create(read.Select(pair => KeyValuePair.Create(pair.Key, (IEnumerable<string>)pair.Value))).Bytes.SequenceEqual(inputs[0].Bytes)) return false;
            domains = read.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray(), StringComparer.Ordinal);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException or InvalidArtifactRenderContract)
        {
            return false;
        }
    }

    /// <summary>
    /// Adds the input to a profile.
    /// </summary>
    /// <param name="profile">A profile that does not already carry a user interface domains input.</param>
    /// <param name="domains">The slice identities by domain sub-path.</param>
    /// <returns>The profile carrying the input.</returns>
    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, IEnumerable<KeyValuePair<string, IEnumerable<string>>> domains) =>
        ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs, Create(domains)]);
}
