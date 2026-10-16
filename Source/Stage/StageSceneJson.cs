// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Stage;

/// <summary>
/// The JSON options a running Stage serves the Scene document with.
/// </summary>
/// <remarks>
/// A generated application is handed the Scene document the renderer planned, in the spelling the TypeScript Scene model
/// reads. A running Stage serves the same document, so it spells it the same way: each enumeration as the model spells it
/// (<c language="shell">dialog</c>, not <c language="shell">Dialog</c>, for a destination), and nothing written for a member that
/// has no value. Reading the other spelling parses and then equals no member, so a destination that opens a dialog would
/// open nothing. The renderer's own spelling lives in <c language="shell">SceneEnumerationNaming</c>, which the Stage cannot
/// reference; a conformance specification pins the two together.
/// </remarks>
public static class StageSceneJson
{
    /// <summary>
    /// The Scene enumerations the TypeScript model declares with PascalCase values; every other one is camelCase.
    /// </summary>
    public static readonly IReadOnlySet<string> PascalCasedEnumerations = new HashSet<string>(StringComparer.Ordinal)
    {
        "ColorScheme",
        "Dock",
        "FlowContainerKind",
        "GridUnitType",
        "HeightSizeClass",
        "HorizontalAlignment",
        "InteractionActionKind",
        "InteractionTriggerKind",
        "NotificationLevel",
        "Orientation",
        "PackageKind",
        "SliceKind",
        "TargetSizeClass",
        "TemplateReferenceSource",
        "TemplateRole",
        "TemplateScope",
        "VerticalAlignment",
        "Visibility",
        "WidthSizeClass",
    };

    /// <summary>
    /// Gets the JSON options the Scene document is served with.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new EnumerationNaming() },
    };

    sealed class EnumerationNaming : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            new JsonStringEnumConverter(PascalCasedEnumerations.Contains(typeToConvert.Name) ? null : JsonNamingPolicy.CamelCase).CreateConverter(typeToConvert, options);
    }
}
