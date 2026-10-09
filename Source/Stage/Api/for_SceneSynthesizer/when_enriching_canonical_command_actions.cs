// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Forms;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_enriching_canonical_command_actions : Specification
{
    SceneApplication _result = null!;

    void Because()
    {
        var screen = new Screen(
            "WorkItemList",
            "AppShell",
            new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal)
            {
                [DefaultLayout.ContentSlotName] =
                [
                    Action("CreateWorkItem"),
                    Action("RenameWorkItem"),
                    Action("AddComment")
                ]
            },
            [
                new Form("CreateWorkItemForm", "CreateWorkItem", null, [], null, null),
                new Form("RenameWorkItemForm", "RenameWorkItem", null, [], null, null),
                new Form("AddCommentForm", "AddComment", null, [], null, null)
            ],
            []);
        var scene = new SceneApplication([], [], [DefaultLayout.Create()], [], [], [screen]);

        _result = SceneSynthesizer.Synthesize(scene, Model());
    }

    [Fact] void should_expose_create_work_item_fields() => FieldsFor("CreateWorkItem").ShouldContainOnly(["workItemId", "title"]);
    [Fact] void should_expose_rename_work_item_fields() => FieldsFor("RenameWorkItem").ShouldContainOnly(["workItemId", "title"]);
    [Fact] void should_expose_add_comment_fields() => FieldsFor("AddComment").ShouldContainOnly(["commentId", "workItemId", "text"]);
    [Fact] void should_keep_all_canonical_command_metadata_available() => Actions.Select(action => action.Properties["metadataStatus"]).ShouldContainOnly(["available", "available", "available"]);

    IReadOnlyList<ExternalComponent> Actions => [.. _result.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].OfType<ExternalComponent>()];

    IReadOnlyList<string> FieldsFor(string command) =>
        [.. ((IReadOnlyList<CommandFormField>)Actions.Single(action => (string)action.Properties["command"]! == command).Properties["fields"]!).Select(field => field.Name)];

    static ExternalComponent Action(string command) => new()
    {
        Id = command,
        Name = command,
        ComponentName = "core:action",
        Properties = new Dictionary<string, object?> { ["command"] = command, ["label"] = command },
        Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
    };

    static EventModel Model()
    {
        var modelId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        return new EventModel(
            modelId,
            "Workspaces",
            [
                new ModuleCollection(
                    collectionId,
                    modelId,
                    [new Module(Guid.NewGuid(), modelId, collectionId, "Workspaces", [new Feature(Guid.NewGuid(), "Tracking", null, [], Slices())])])
            ]);
    }

    static IReadOnlyList<Slice> Slices() =>
    [
        Slice("CreateWorkItem", """{"type":"object","properties":{"workItemId":{"type":"string","format":"uuid"},"title":{"type":"string"}},"required":["workItemId","title"]}"""),
        Slice("RenameWorkItem", """{"type":"object","properties":{"workItemId":{"type":"string","format":"uuid"},"title":{"type":"string"}},"required":["workItemId","title"]}"""),
        Slice("AddComment", """{"type":"object","properties":{"commentId":{"type":"string","format":"uuid"},"workItemId":{"type":"string","format":"uuid"},"text":{"type":"string"}},"required":["commentId","workItemId","text"]}""")
    ];

    static Slice Slice(string name, string schema) => new(
        Guid.NewGuid(),
        name,
        SliceType.StateChange,
        [],
        new CommandDefinition(Guid.NewGuid(), name, schema, SchemaSynthesizer.EmptyObjectSchema, [], string.Empty, []),
        null,
        []);
}
