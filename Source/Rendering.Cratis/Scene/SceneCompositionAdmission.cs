// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Validates the wire shape of an authored Scene input before it can become a generated artifact.
/// </summary>
internal static class SceneCompositionAdmission
{
    static readonly string[] _collections = ["uiProfiles", "themes", "layouts", "screenTemplates", "dialogTemplates", "screens"];

    /// <summary>
    /// Checks the input version and the JSON structure consumed by the generated frontend.
    /// </summary>
    /// <param name="input">The composed Scene input.</param>
    /// <param name="mismatch">The reason admission failed, without including payload values.</param>
    /// <returns>Whether the input has an admitted version and wire shape.</returns>
    public static bool Matches(ArtifactRenderInput input, out string mismatch)
    {
        if (!string.Equals(input.Version, CratisRendering.TargetVersion, StringComparison.Ordinal))
        {
            mismatch = "The composed Scene input version does not match the Cratis target version.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(input.Bytes.AsSpan()));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || HasDuplicateMembers(root) ||
                !_collections.All(name => root.TryGetProperty(name, out var collection) && collection.ValueKind == JsonValueKind.Array) ||
                !root.GetProperty("screens").EnumerateArray().All(IsScreen))
            {
                mismatch = "The composed Scene payload must contain the Scene application collections and screens with object-valued slot content and arrays of elements, without duplicate JSON members.";
                return false;
            }
        }
        catch (Exception exception) when (exception is DecoderFallbackException or JsonException)
        {
            mismatch = "The composed Scene payload is not valid UTF-8 JSON.";
            return false;
        }

        mismatch = string.Empty;
        return true;
    }

    static bool IsScreen(JsonElement screen) =>
        screen.ValueKind == JsonValueKind.Object &&
        screen.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String &&
        screen.TryGetProperty("slotContent", out var slots) && slots.ValueKind == JsonValueKind.Object &&
        slots.EnumerateObject().All(slot => slot.Value.ValueKind == JsonValueKind.Array &&
            slot.Value.EnumerateArray().All(element => element.ValueKind == JsonValueKind.Object));

    static bool HasDuplicateMembers(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().GroupBy(property => property.Name, StringComparer.Ordinal).Any(group => group.Count() > 1) ||
            value.EnumerateObject().Any(property => HasDuplicateMembers(property.Value)),
        JsonValueKind.Array => value.EnumerateArray().Any(HasDuplicateMembers),
        _ => false
    };
}
