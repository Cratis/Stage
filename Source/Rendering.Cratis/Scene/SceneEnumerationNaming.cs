// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Writes each Scene enumeration the way the TypeScript Scene model spells it.
/// </summary>
/// <remarks>
/// The TypeScript model is not consistent: <c language="shell">BindingSourceKind</c> is <c language="shell">dataContext</c> but
/// <c language="shell">InteractionTriggerKind</c> is <c language="shell">DoubleClick</c>, and the Scene engine compares both with strict equality.
/// A value spelled the other way parses and then equals no member, so a trigger never matches and a layout container is
/// never a row - silently, in every generated application. The set below is the TypeScript enumerations whose values are
/// PascalCase; everything else is camelCase. <c language="shell">SceneEnumerationNamingSpecs</c> pins both lists against the
/// Scene model assembly, so a new enumeration cannot slip through unclassified.
/// </remarks>
public sealed class SceneEnumerationNaming : JsonConverterFactory
{
    /// <summary>
    /// The Scene enumerations the TypeScript model declares with PascalCase values.
    /// </summary>
    public static readonly IReadOnlySet<string> PascalCased = new HashSet<string>(StringComparer.Ordinal)
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

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        new JsonStringEnumConverter(PascalCased.Contains(typeToConvert.Name) ? null : JsonNamingPolicy.CamelCase).CreateConverter(typeToConvert, options);
}
