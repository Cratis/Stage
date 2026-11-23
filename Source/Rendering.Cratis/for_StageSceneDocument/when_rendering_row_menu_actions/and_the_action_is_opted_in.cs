// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StageSceneDocument.when_rendering_row_menu_actions;

public class and_the_action_is_opted_in : Specification
{
    static readonly string FixturePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Frontend/Runtime/src/fixtures/row-menu-actions-scene.json"));
    JsonNode _live = null!;
    JsonNode _rendered = null!;
    string _json = null!;

    void Because()
    {
        var syntax = new ScreenplayCompiler().Compile(Source);
        Assert.True(syntax.Success, string.Join(Environment.NewLine, syntax.Diagnostics));
        var conversion = StageSceneConversion.Convert(syntax.Value!);
        var errors = conversion.ConversionDiagnostics.Where(diagnostic => diagnostic.Severity == global::Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(error => $"{error.Code}: {error.Message}")));
        var canonical = CanonicalSceneJson.Serialize(conversion);
        _live = JsonNode.Parse(canonical)!;
        Assert.NotEmpty(_live["screens"]!.AsArray());
        Assert.Equal("row-menu", Action(_live, "Rename")["presentation"]!["placement"]!.GetValue<string>());

        var model = invoice_model.Compile(Source);
        var profile = CratisRendering.CreateProfile("Resources", new("Resources", "Resources"));
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Resources", "Resources"));
        _rendered = JsonNode.Parse(StageSceneDocument.Render(canonical, context))!;
        _json = _rendered.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
        if (Environment.GetEnvironmentVariable("STAGE_UPDATE_VECTORS") == "1") File.WriteAllText(FixturePath, _json);
    }

    [Fact] void should_match_the_runtime_acceptance_fixture() => File.ReadAllText(FixturePath).ShouldEqual(_json);
    [Fact] void should_preserve_the_live_hint() => Action(_live, "Rename")["presentation"]!["placement"]!.GetValue<string>().ShouldEqual("row-menu");
    [Fact] void should_preserve_the_emitted_hint() => Action(_rendered, "Archive")["presentation"]!["placement"]!.GetValue<string>().ShouldEqual("row-menu");
    [Fact] void should_preserve_advisory_metadata() => Action(_rendered, "Rename")["presentation"]!["tone"]!.GetValue<string>().ShouldEqual("quiet");
    [Fact] void should_keep_an_unhinted_item_action_unhinted() => (Action(_rendered, "Inline rename")["presentation"] is null).ShouldBeTrue();
    [Fact] void should_not_reinterpret_an_unsupported_placement() => Action(_rendered, "Advisory rename")["presentation"]!["placement"]!.GetValue<string>().ShouldEqual("menu");
    [Fact] void should_keep_unknown_advisory_values() => Action(_rendered, "Unknown placement")["presentation"]!["placement"]!.GetValue<string>().ShouldEqual("radial");
    [Fact] void should_keep_the_command() => Action(_rendered, "Rename")["command"]!.GetValue<string>().ShouldEqual("RenameResource");
    [Fact] void should_keep_the_item_subject() => Action(_rendered, "Rename")["actionSubject"]!.GetValue<string>().ShouldEqual("item");
    [Fact] void should_keep_subject_inputs() => Action(_rendered, "Rename")["subjectInputs"]!.ToJsonString().ShouldEqual(Action(_live, "Rename")["subjectInputs"]!.ToJsonString());
    [Fact] void should_keep_navigation() => Action(_rendered, "Rename")["navigateToScreen"]!.GetValue<string>().ShouldEqual("Resources");
    [Fact] void should_enrich_the_same_action_with_its_schema() => JsonNode.Parse(Action(_rendered, "Rename")["schema"]!.GetValue<string>())!["properties"]!["resourceId"].ShouldNotBeNull();

    static JsonObject Action(JsonNode document, string label) => Objects(document)
        .Where(node => node["componentName"]?.GetValue<string>() == "core:action")
        .Select(node => node["properties"]!.AsObject())
        .Single(properties => properties["label"]?.GetValue<string>() == label);

    static IEnumerable<JsonObject> Objects(JsonNode? node)
    {
        if (node is JsonObject value)
        {
            yield return value;
            foreach (var child in value.SelectMany(property => Objects(property.Value))) yield return child;
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array.SelectMany(Objects)) yield return child;
        }
    }

    const string Source = """
        concept ResourceId : Uuid
        module Business
          feature Inventory
            screen Resources
              table Resource as AllResources
                column name label "Name"
              action RegisterResource for screen
                label "Create"
              action RenameResource for item
                label "Rename"
                with resourceId from item.resourceId
                navigate to Resources by resourceId
                presentation placement "row-menu"
                presentation tone "quiet"
              action ArchiveResource for item
                label "Archive"
                with resourceId from item.resourceId
                presentation placement "row-menu"
              action RenameResource for item
                label "Inline rename"
                with resourceId from item.resourceId
              action RenameResource for item
                label "Advisory rename"
                with resourceId from item.resourceId
                presentation placement "menu"
              action RenameResource for item
                label "Unknown placement"
                with resourceId from item.resourceId
                presentation placement "radial"
            slice StateChange RegisterResource
              command RegisterResource
                resourceId ResourceId identifier
                name String
                produces ResourceRegistered
                  for resourceId
                  name = name
              event ResourceRegistered
                name String
            slice StateChange RenameResource
              command RenameResource
                resourceId ResourceId identifier
                name String
                produces ResourceRenamed
                  for resourceId
                  name = name
              event ResourceRenamed
                name String
            slice StateChange ArchiveResource
              command ArchiveResource
                resourceId ResourceId identifier
                reason String
                produces ResourceArchived
                  for resourceId
                  reason = reason
              event ResourceArchived
                reason String
            slice StateView Resources
              readmodel Resource
                resourceId ResourceId
                name String
              query AllResources => Resource[]
              uses screen Resources
              projection ResourcesProjection => Resource
                from ResourceRegistered
                  resourceId = $eventSourceId
                  name = name
                from ResourceRenamed
                  name = name
        """;
}
