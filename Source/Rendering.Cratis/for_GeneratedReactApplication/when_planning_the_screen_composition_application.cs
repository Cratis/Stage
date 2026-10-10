// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// Planning the canonical screen-composition corpus into a generated React application, twice.
/// </summary>
/// <remarks>
/// The planned artifacts are the whole generated application before its build: the frontend shell, the Stage runtime,
/// the Scene document with its shells, templates, dialogs, navigation and forms, and the module binding the generated
/// proxies' routes. They must be the same bytes on every run and carry nothing of the machine that planned them.
/// </remarks>
public class when_planning_the_screen_composition_application : a_screen_composition_render
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;
    JsonDocument _document = null!;
    string _allText = null!;

    void Because()
    {
        _first = Plan();
        _second = Plan();
        _document = JsonDocument.Parse(Text(_first, "src/stage-scene.json"));
        _allText = string.Join('\n', _first.Artifacts.Select(_ => System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan())));
    }

    [Fact] void should_plan_without_diagnostics() => _first.Diagnostics.ShouldBeEmpty();
    [Fact] void should_plan_the_same_artifact_paths_twice() => _second.Artifacts.Select(_ => _.RelativePath).SequenceEqual(_first.Artifacts.Select(_ => _.RelativePath)).ShouldBeTrue();
    [Fact] void should_plan_the_same_artifact_bytes_twice() => _second.Artifacts.Zip(_first.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_plan_the_same_artifact_hashes_twice() => _second.Artifacts.Select(_ => _.Sha256).SequenceEqual(_first.Artifacts.Select(_ => _.Sha256)).ShouldBeTrue();
    [Fact] void should_emit_the_stage_runtime() => Paths().ShouldContain(".frontend/stage/App.tsx", ".frontend/stage/stageSource.ts", ".frontend/stage/StageCommandForm.tsx", ".frontend/stage/stageNavigation.ts", ".frontend/styledPrimeReact.ts");
    [Fact] void should_emit_the_runtime_document_and_module() => Paths().ShouldContain("scene.json", "src/bindings.ts", "src/stage-scene.json", "src/stage.ts");
    [Fact] void should_not_emit_the_live_stage_entry_point_or_its_specifications() => Paths().Any(path => path.StartsWith(".frontend/stage/", StringComparison.Ordinal) && (path.Contains(".spec.", StringComparison.Ordinal) || path.EndsWith("/main.tsx", StringComparison.Ordinal) || path.EndsWith("/testSetup.ts", StringComparison.Ordinal))).ShouldBeFalse();
    [Fact] void should_mount_the_runtime_over_the_planned_content() => Text(_first, ".frontend/main.tsx").ShouldContain("<StageSourceProvider source={source}>");
    [Fact] void should_load_the_icon_font() => Text(_first, ".frontend/main.tsx").ShouldContain("import 'primeicons/primeicons.css';");
    [Fact] void should_carry_the_application_shell() => Names("layouts").ShouldContainOnly("AppShell");
    [Fact] void should_carry_the_screen_template() => Names("screenTemplates").ShouldContainOnly("MasterDetail");
    [Fact] void should_carry_the_dialog_template() => Names("dialogTemplates").ShouldContainOnly("EditDialog");
    [Fact] void should_carry_every_screen() => Names("screens").ShouldContainOnly("CommentThread", "WorkItemDetails", "WorkItemList");
    [Fact] void should_carry_the_navigation_contribution() => Screens().All(screen => screen.GetProperty("contributions").EnumerateArray().Any(_ => _.GetProperty("contributionPointName").GetString() == "Navigation")).ShouldBeTrue();
    [Fact] void should_place_the_authored_forms_as_native_command_forms() => FormCommands().ShouldContainOnly("CreateWorkItem", "RenameWorkItem");
    [Fact] void should_give_every_form_and_action_its_command_schema() => CommandElements().All(_ => _.GetProperty("properties").GetProperty("metadataStatus").GetString() == "available" && _.GetProperty("properties").TryGetProperty("schema", out _)).ShouldBeTrue();
    [Fact] void should_bind_the_row_click_navigation() => _allText.ShouldContain("\"navigateOnRowClickToScreen\":\"WorkItemDetails\"");
    [Fact] void should_bind_scoped_query_arguments() => Text(_first, "src/stage-scene.json").ShouldContain("\"query\":\"CommentsForWorkItem\"");
    [Fact] void should_read_every_route_from_the_generated_proxies() => Text(_first, "src/stage.ts").ShouldContain("\"AddComment\": new __stageCommand0().route,");
    [Fact] void should_not_guess_any_route() => Text(_first, "src/stage-scene.json").ShouldNotContain("/api/");
    [Fact] void should_not_carry_a_guarded_action() => Text(_first, "src/stage-scene.json").Contains("\"otherwise\"", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_offer_the_guarded_close_command() => CommandElements().Any(_ => _.GetProperty("properties").GetProperty("command").GetString() == "CloseWorkItem").ShouldBeFalse();
    [Fact] void should_not_carry_a_timestamp() => HasTimestamp(_allText).ShouldBeFalse();
    [Fact] void should_check_the_guids_the_corpus_specifications_declare() => Guids(_allText).ShouldContain("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    [Fact] void should_not_carry_a_guid_the_corpus_does_not_declare() => UndeclaredGuids().ShouldBeEmpty();
    [Fact] void should_not_carry_the_machine_user_or_paths() => EnvironmentValues().Where(value => _allText.Contains(value, StringComparison.OrdinalIgnoreCase)).ShouldBeEmpty();

    string[] Paths() => [.. _first.Artifacts.Select(_ => _.RelativePath)];

    JsonElement[] Screens() => [.. _document.RootElement.GetProperty("screens").EnumerateArray()];

    string[] Names(string collection) => [.. _document.RootElement.GetProperty(collection).EnumerateArray().Select(_ => _.GetProperty("name").GetString()!)];

    string[] FormCommands() => [.. Screens()
        .SelectMany(screen => screen.GetProperty("slotContent").TryGetProperty("forms", out var forms) ? forms.EnumerateArray() : Enumerable.Empty<JsonElement>())
        .Where(_ => _.GetProperty("componentName").GetString() == "Stage:commandForm")
        .Select(_ => _.GetProperty("properties").GetProperty("command").GetString()!)
        .Distinct(StringComparer.Ordinal)];

    JsonElement[] CommandElements() => [.. Elements(_document.RootElement).Where(_ =>
        new[] { "core:action", "Stage:commandForm" }.Contains(_.GetProperty("componentName").GetString(), StringComparer.Ordinal) &&
        _.GetProperty("properties").TryGetProperty("command", out _))];

    static IEnumerable<JsonElement> Elements(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => (value.TryGetProperty("componentName", out _) ? [value] : Enumerable.Empty<JsonElement>())
            .Concat(value.EnumerateObject().SelectMany(property => Elements(property.Value))),
        JsonValueKind.Array => value.EnumerateArray().SelectMany(Elements),
        _ => []
    };

    string[] UndeclaredGuids()
    {
        var declared = string.Join('\n', Folder.Documents.Select(_ => _.Text));
        return [.. Guids(_allText).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(guid => !declared.Contains(guid, StringComparison.OrdinalIgnoreCase))];
    }

    static string[] EnvironmentValues() => [.. new[]
    {
        Environment.MachineName,
        Environment.UserName,
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
        Environment.CurrentDirectory,
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)
    }.Where(value => value.Length > 3)];

    // An ISO 8601 date and time, as a build or generation stamp writes one: dddd-dd-ddTdd:dd.
    static bool HasTimestamp(string text) =>
        Enumerable.Range(0, Math.Max(0, text.Length - 15)).Any(index => Matches(text, index, "dddd-dd-ddTdd:dd"));

    // A dashed GUID: 8-4-4-4-12 hexadecimal digits, not inside a longer run of hexadecimal digits.
    static IEnumerable<string> Guids(string text) =>
        Enumerable.Range(0, Math.Max(0, text.Length - 35))
            .Where(index => Matches(text, index, "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx") &&
                (index == 0 || !Uri.IsHexDigit(text[index - 1])) &&
                (index + 36 == text.Length || !Uri.IsHexDigit(text[index + 36])))
            .Select(index => text.Substring(index, 36));

    // 'd' is a decimal digit, 'x' a hexadecimal digit, anything else itself.
    static bool Matches(string text, int index, string shape) =>
        index + shape.Length <= text.Length &&
        shape.Select((expected, offset) => expected switch
        {
            'd' => char.IsAsciiDigit(text[index + offset]),
            'x' => Uri.IsHexDigit(text[index + offset]),
            _ => text[index + offset] == expected
        }).All(matched => matched);
}
