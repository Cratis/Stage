// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_an_authored_action_has_no_command_metadata : given.a_scene_model
{
    SceneApplication _result = null!;

    void Because()
    {
        var action = new ExternalComponent
        {
            Id = "archive-project",
            Name = "Archive project",
            ComponentName = "core:action",
            Properties = new Dictionary<string, object?> { ["command"] = "ArchiveProject" },
            Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
        };
        var screen = new Screen(
            "ProjectArchive",
            _scene.Layouts[0].Name,
            new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal) { [DefaultLayout.ContentSlotName] = [action] },
            [],
            []);

        _result = SceneSynthesizer.Synthesize(_scene with { Screens = [screen] }, _semanticModel);
    }

    [Fact] void should_mark_the_metadata_as_missing() => ActionProperties["metadataStatus"].ShouldEqual("missing");
    [Fact] void should_explain_that_stage_will_not_submit_an_empty_payload() => ((string)ActionProperties["metadataError"]!).ShouldContain("will not submit an empty payload");
    [Fact] void should_not_claim_a_schema_exists() => ActionProperties.ContainsKey(SceneSynthesizer.SchemaProperty).ShouldBeFalse();
    [Fact] void should_not_claim_fields_exist() => ActionProperties.ContainsKey("fields").ShouldBeFalse();

    IReadOnlyDictionary<string, object?> ActionProperties => _result.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].OfType<ExternalComponent>().Single().Properties;
}
