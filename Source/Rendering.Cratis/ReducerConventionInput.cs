// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Resolves application-owned reducer rendering configuration without destination discovery or migration.
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
    /// <returns>The validated, canonical version-one input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The configuration is malformed, stale or unsupported.</exception>
    public static ArtifactRenderInput Read(string applicationName, string json)
    {
        if (!TryParse(json, applicationName, out var convention))
        {
            throw new InvalidArtifactRenderContract("Reducer configuration requires version 1, the matching application, and reducerConvention 'classic' or 'self'; overrides and migration are not supported.");
        }

        return Create(applicationName, convention);
    }

    internal static ArtifactRenderInput Create(string applicationName, string convention) =>
        ArtifactRenderInput.Create(Name, "1", [.. Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { version = 1, application = applicationName, reducerConvention = convention }) + "\n")]);

    internal static bool TryRead(ArtifactRenderProfile profile, string applicationName, out bool self)
    {
        self = false;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length != 1 || inputs[0].Version != "1") return false;
        try
        {
            if (!TryParse(new UTF8Encoding(false, true).GetString(inputs[0].Bytes.AsSpan()), applicationName, out var convention)) return false;
            self = convention == "self";
            return inputs[0].Bytes.SequenceEqual(Create(applicationName, convention).Bytes);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    static bool TryParse(string json, string applicationName, out string convention)
    {
        convention = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            var properties = root.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
            if (!properties.SequenceEqual(["application", "reducerConvention", "version"])) return false;
            if (!root.GetProperty("version").TryGetInt32(out var version) || version != 1 ||
                root.GetProperty("application").GetString() != applicationName)
            {
                return false;
            }
            convention = root.GetProperty("reducerConvention").GetString() ?? string.Empty;
            return string.Equals(convention, "classic", StringComparison.Ordinal) || string.Equals(convention, "self", StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            return false;
        }
    }
}
