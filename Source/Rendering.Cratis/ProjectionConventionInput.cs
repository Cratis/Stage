// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Persists the projection convention explicitly selected for a new application.
/// </summary>
/// <remarks>Model-bound projections use read-model identities. This configuration must not migrate an existing application.</remarks>
public static class ProjectionConventionInput
{
    /// <summary>
    /// Gets the profile input identity.
    /// </summary>
    public const string Name = "cratis-application:projection-convention";

    /// <summary>
    /// Gets the application-owned configuration path.
    /// </summary>
    public const string RelativePath = "stage.projections.json";

    /// <summary>
    /// Reads and canonicalizes an application's explicitly selected projection convention.
    /// </summary>
    /// <param name="applicationName">The expected application identity.</param>
    /// <param name="json">The versioned application configuration.</param>
    /// <returns>The immutable profile input.</returns>
    /// <exception cref="InvalidArtifactRenderContract">The configuration is malformed or belongs to another application.</exception>
    public static ArtifactRenderInput Read(string applicationName, string json)
    {
        if (!TryParse(json, applicationName, out var modelBound))
        {
            throw new InvalidArtifactRenderContract("The projection convention configuration is malformed, stale or unsupported.");
        }

        return Create(applicationName, modelBound ? "model-bound" : "classic");
    }

    internal static ArtifactRenderInput Create(string applicationName, string convention) =>
        ArtifactRenderInput.Create(Name, "1", [.. Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { version = 1, application = applicationName, projectionConvention = convention }) + "\n")]);

    internal static bool TryRead(ArtifactRenderProfile profile, string applicationName, out bool modelBound)
    {
        modelBound = false;
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        return inputs.Length == 0 || (inputs.Length == 1 && inputs[0].Version == "1" &&
            TryParse(Encoding.UTF8.GetString(inputs[0].Bytes.AsSpan()), applicationName, out modelBound));
    }

    static bool TryParse(string json, string applicationName, out bool modelBound)
    {
        modelBound = false;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var properties = root.EnumerateObject().ToArray();
            if (properties.Length != 3 || properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != 3 ||
                !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number) || number != 1 ||
                !root.TryGetProperty("application", out var application) || application.ValueKind != JsonValueKind.String || application.GetString() != applicationName ||
                !root.TryGetProperty("projectionConvention", out var convention) || convention.ValueKind != JsonValueKind.String || convention.GetString() is not ("classic" or "model-bound"))
            {
                return false;
            }

            modelBound = convention.GetString() == "model-bound";
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
