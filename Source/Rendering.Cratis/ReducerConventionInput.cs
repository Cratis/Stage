// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Resolves application-owned reducer rendering configuration, including read models migrated to self-reduction.
/// </summary>
public static class ReducerConventionInput
{
    /// <summary>
    /// The versioned profile input identity.
    /// </summary>
    public const string Name = "cratis-rendering:reducer-convention";

    /// <summary>
    /// The application configuration path persisted by scaffold planning.
    /// </summary>
    public const string RelativePath = ".cratis/stage/rendering.json";

    /// <summary>
    /// Converts persisted configuration to a canonical profile input for the named application.
    /// </summary>
    /// <param name="applicationName">The exact semantic application name.</param>
    /// <param name="json">The persisted configuration contents.</param>
    /// <returns>The validated, canonical input: version one without migrated read models, version two with them.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The configuration is malformed, stale or unsupported.</exception>
    public static ArtifactRenderInput Read(string applicationName, string json)
    {
        if (!TryParse(json, applicationName, out var convention))
        {
            throw new InvalidArtifactRenderContract("Reducer configuration requires the matching application and reducerConvention 'classic' or 'self': version 1 without read-model overrides, or version 2 with ordinally sorted, unique self overrides carrying an observer id and previous path.");
        }

        return Create(applicationName, convention);
    }

    internal static ArtifactRenderInput Create(string applicationName, string convention) =>
        Create(applicationName, ReducerConvention.Classic with { SelfDefault = convention == "self" });

    internal static ArtifactRenderInput Create(string applicationName, ReducerConvention convention)
    {
        var name = convention.SelfDefault ? "self" : "classic";
        if (convention.Overrides.IsEmpty)
        {
            return ArtifactRenderInput.Create(Name, "1", Encode(new { version = 1, application = applicationName, reducerConvention = name }));
        }

        var readModels = convention.Overrides.OrderBy(entry => entry.Key.ToString(), StringComparer.Ordinal)
            .Select(entry => new { id = entry.Key.ToString(), convention = "self", observerId = entry.Value.ObserverId, previousPath = entry.Value.PreviousPath });
        return ArtifactRenderInput.Create(Name, "2", Encode(new { version = 2, application = applicationName, reducerConvention = name, readModels }));
    }

    internal static bool TryRead(ArtifactRenderProfile profile, string applicationName, out ReducerConvention convention)
    {
        convention = ReducerConvention.Classic;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length != 1) return false;
        try
        {
            if (!TryParse(new UTF8Encoding(false, true).GetString(inputs[0].Bytes.AsSpan()), applicationName, out var parsed)) return false;
            var canonical = Create(applicationName, parsed);
            if (inputs[0].Version != canonical.Version || !inputs[0].Bytes.SequenceEqual(canonical.Bytes)) return false;
            convention = parsed;
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    internal static bool TryParse(string json, string applicationName, out ReducerConvention convention)
    {
        convention = ReducerConvention.Classic;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("version", out var versionElement) || !versionElement.TryGetInt32(out var version)) return false;
            string[] expected = version == 2 ? ["application", "readModels", "reducerConvention", "version"] : ["application", "reducerConvention", "version"];
            if ((version != 1 && version != 2) || !HasExactProperties(root, expected) || root.GetProperty("application").GetString() != applicationName) return false;
            var name = root.GetProperty("reducerConvention").GetString();
            if (name is not ("classic" or "self")) return false;
            var overrides = ImmutableDictionary<SemanticId, ReducerOverride>.Empty;
            if (version == 2 && !TryParseOverrides(root.GetProperty("readModels"), out overrides)) return false;
            convention = new(name == "self", overrides);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
        {
            return false;
        }
    }

    static ImmutableArray<byte> Encode(object value) => [.. Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value) + "\n")];

    static bool TryParseOverrides(JsonElement readModels, out ImmutableDictionary<SemanticId, ReducerOverride> overrides)
    {
        overrides = [];
        if (readModels.ValueKind != JsonValueKind.Array || readModels.GetArrayLength() == 0)
        {
            return false;
        }

        var previous = string.Empty;
        var builder = ImmutableDictionary.CreateBuilder<SemanticId, ReducerOverride>();
        foreach (var entry in readModels.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object || !HasExactProperties(entry, ["convention", "id", "observerId", "previousPath"]) ||
                entry.GetProperty("convention").GetString() != "self")
            {
                return false;
            }

            var id = entry.GetProperty("id").GetString() ?? string.Empty;
            var observerId = entry.GetProperty("observerId").GetString();
            var previousPath = entry.GetProperty("previousPath").GetString();

            // Strict ordinal order also rejects duplicates, so one canonical byte form exists per configuration.
            if (string.CompareOrdinal(previous, id) >= 0 || !SemanticId.TryParse(id, out var readModel) ||
                string.IsNullOrWhiteSpace(observerId) || !IsPortablePath(previousPath))
            {
                return false;
            }

            previous = id;
            builder.Add(readModel, new(observerId, previousPath!));
        }

        overrides = builder.ToImmutable();
        return true;
    }

    static bool HasExactProperties(JsonElement element, string[] expected) =>
        element.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).SequenceEqual(expected);

    static bool IsPortablePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".cs", StringComparison.Ordinal)) return false;
        try
        {
            return PlannedArtifact.CreateText(path, string.Empty).RelativePath == path;
        }
        catch (InvalidArtifactRenderContract)
        {
            return false;
        }
    }
}
