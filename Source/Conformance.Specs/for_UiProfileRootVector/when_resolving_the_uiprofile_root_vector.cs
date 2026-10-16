// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Files;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_UiProfileRootVector;

/// <summary>
/// The corpus' <c>ui-profile-root-positive</c> document - an own layout root with options, appearance and a new-screens default, an own
/// application-scope screen template root and a blueprint root with a version pin - carried through Screenplay's compiler, Stage's
/// converter and the Scene engine's shell resolver, and held to checked-in expectations the TypeScript engine is held to as well
/// (<c>Source/Frontend/src/uiProfileRootVector.spec.ts</c>): the translated Scene, and what each profile's effective shell resolves to.
/// </summary>
/// <remarks>
/// Set <c>STAGE_UPDATE_VECTORS=1</c> to rewrite the two expectation files after a deliberate change to the corpus or the converter.
/// </remarks>
public class when_resolving_the_uiprofile_root_vector : Specification
{
    const string SceneFile = "uiprofile-root.scene.json";
    const string ShellsFile = "uiprofile-root.shells.json";

    SceneApplication _scene = null!;
    ApplicationRenderPlan _plan = null!;
    string _sceneJson = null!;
    string _shellsJson = null!;

    void Because()
    {
        _scene = SceneOfProfiles(ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "ui-profile-root-positive").Document);
        _plan = RenderPlanner.Plan(_scene, CratisRendering.CatalogWith(null));
        _sceneJson = Normalize(CanonicalSceneJson.Serialize(_scene));
        _shellsJson = ShellsOf(_plan);

        if (Environment.GetEnvironmentVariable("STAGE_UPDATE_VECTORS") == "1")
        {
            var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "for_UiProfileRootVector"));
            File.WriteAllText(Path.Combine(folder, SceneFile), _sceneJson);
            File.WriteAllText(Path.Combine(folder, ShellsFile), _shellsJson);
        }
    }

    [Fact] void should_translate_the_three_profiles() => _scene.UiProfiles.Select(profile => profile.Name).ShouldEqual(["Desktop", "Docked", "FromBlueprint"]);
    [Fact] void should_carry_the_root_template_of_each_profile() => _scene.UiProfiles.All(profile => profile.Root is not null).ShouldBeTrue();
    [Fact] void should_carry_the_shell_options_with_their_kinds() => _scene.UiProfiles[0].Root!.Options!["showFooter"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_carry_the_appearance() => _scene.UiProfiles[0].Appearance!.Accent.ShouldEqual("#4F46E5");
    [Fact] void should_carry_the_template_new_screens_start_from() => _scene.UiProfiles[0].Defaults!.NewScreenTemplate!.Id.ShouldEqual("Sheet");
    [Fact] void should_carry_the_pin_of_a_blueprint_root() => _scene.UiProfiles[2].Root!.Template.Version.ShouldEqual("^4.5");
    [Fact] void should_translate_to_the_checked_in_scene() => _sceneJson.ShouldEqual(Normalize(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "for_UiProfileRootVector", SceneFile))));
    [Fact] void should_resolve_each_profile_to_the_checked_in_shell() => _shellsJson.ShouldEqual(Normalize(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "for_UiProfileRootVector", ShellsFile))));

    // A ui profile becomes one Scene profile per `target platform`, and the corpus' second and third profile name none: they are
    // tested for what they say about the root template, so each is given the platform the first one targets.
    static SceneApplication SceneOfProfiles(CanonicalCorpusDocument document)
    {
        var text = Encoding.UTF8.GetString([.. document.Bytes]).Replace("ui profile Docked\n", "ui profile Docked\n  target platform web\n", StringComparison.Ordinal).Replace("ui profile FromBlueprint\n", "ui profile FromBlueprint\n  target platform web\n", StringComparison.Ordinal);
        var root = Path.Combine(Path.GetTempPath(), $"stage-uiprofile-root-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "application.play"), text);
            var compilation = new PlayFileCompiler().CompileFolder(root).Result;
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return new ScreenplaySceneVisitor().Visit(compilation.Value!);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string Normalize(string text) => text.ReplaceLineEndings("\n").TrimEnd() + "\n";

    // What a shell resolves to, in the shape the TypeScript spec projects the same engine's answer to: the facts a renderer acts on.
    static string ShellsOf(ApplicationRenderPlan plan)
    {
        var shells = new JsonObject();
        foreach (var target in plan.Targets)
        {
            var shell = target.Shell!;
            shells[target.Profile.Name] = new JsonObject
            {
                ["name"] = shell.Name,
                ["kind"] = Camel(shell.Kind),
                ["source"] = Camel(shell.Source),
                ["origin"] = Camel(shell.Origin),
                ["primaryOutlet"] = shell.PrimaryOutlet,
                ["options"] = JsonNode.Parse(JsonSerializer.Serialize(shell.Options.OrderBy(option => option.Key, StringComparer.Ordinal).ToDictionary(option => option.Key, option => option.Value))),
                ["diagnostics"] = new JsonArray([.. shell.Diagnostics.Select(diagnostic => (JsonNode)JsonValue.Create(Camel(diagnostic.Code))!)])
            };
        }

        return Normalize(shells.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    static string Camel(object value) => JsonNamingPolicy.CamelCase.ConvertName(value.ToString()!);
}
