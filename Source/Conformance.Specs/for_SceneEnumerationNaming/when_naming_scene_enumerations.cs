// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_SceneEnumerationNaming;

/// <summary>
/// Pins how each Scene enumeration is spelled against the declarations of the TypeScript Scene model that reads it.
/// </summary>
public class when_naming_scene_enumerations : Specification
{
    static readonly JsonSerializerOptions _options = new() { Converters = { new SceneEnumerationNaming() } };

    // Every enumeration the TypeScript Scene model declares, with its first value, read from the installed package.
    static readonly Dictionary<string, string> _declared = TypeScriptEnumerations();

    [Fact] void should_find_the_enumerations_the_typescript_model_declares() => _declared.Count.ShouldBeGreaterThan(30);

    [Fact]
    void should_spell_every_declared_enumeration_as_typescript_does()
    {
        var model = typeof(Cratis.Scene.Model.Elements.SceneElement).Assembly;
        var mismatches = new List<string>();
        foreach (var type in model.GetTypes().Where(candidate => candidate.IsEnum && candidate.IsPublic && _declared.ContainsKey(candidate.Name)))
        {
            var first = Enum.GetValues(type).GetValue(0)!;
            var written = JsonSerializer.Serialize(first, type, _options).Trim('"');
            var expected = _declared[type.Name];
            if (!string.Equals(written, expected, StringComparison.Ordinal))
            {
                mismatches.Add($"{type.Name}: wrote '{written}', the TypeScript model declares '{expected}'");
            }
        }

        mismatches.ShouldBeEmpty();
    }

    [Fact] void should_spell_a_trigger_as_the_engine_matches_it() => JsonSerializer.Serialize(Cratis.Scene.Model.Interactions.InteractionTriggerKind.DoubleClick, _options).ShouldEqual("\"DoubleClick\"");
    [Fact] void should_spell_a_binding_source_as_the_engine_matches_it() => JsonSerializer.Serialize(Cratis.Scene.Model.Common.BindingSourceKind.DataContext, _options).ShouldEqual("\"dataContext\"");

    // Checked in beside the spec and verified against the installed @cratis/scene.model by the frontend's
    // sceneEnumerationNaming spec, so this runs wherever the .NET specs run.
    static Dictionary<string, string> TypeScriptEnumerations() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "for_SceneEnumerationNaming", "scene-model-enumerations.json")))!;
}
