// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_an_application_without_screens : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;
    JsonElement[] _elements = null!;
    JsonElement _properties;
    JsonElement _input;
    string[] _profileHashes = null!;

    void Because()
    {
        _profileHashes = [.. _request.Profile.Inputs.Select(_ => _.Sha256)];
        _plan = _planner.Plan(_request);
        using var document = JsonDocument.Parse(Text(Artifact(SceneCompositionInput.RelativePath)!));
        _elements = [.. document.RootElement.GetProperty("screens")[0].GetProperty("slotContent")
            .GetProperty(DefaultLayout.ContentSlotName).EnumerateArray().Select(_ => _.Clone())];
        _properties = _elements.Single(_ => _.GetProperty("componentName").GetString() == "Cratis.Components:queryInputForm").GetProperty("properties");
        _input = _properties.GetProperty("inputs").EnumerateArray().Single();
    }

    [Fact] void should_plan_without_errors() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_emit_the_binding_module() => Artifact(SceneBindingsRenderer.RelativePath).ShouldNotBeNull();
    [Fact] void should_keep_the_command_form() => _elements.Single(_ => _.GetProperty("componentName").GetString() == "Cratis.Components:commandForm").GetProperty("properties").GetProperty("command").GetString().ShouldEqual("RegisterProject");
    [Fact] void should_match_the_emitted_command_members_and_native_proxy_descriptors()
    {
        var command = _elements.Single(_ => _.GetProperty("componentName").GetString() == "Cratis.Components:commandForm").GetProperty("properties");
        command.GetProperty("submitLabel").GetString().ShouldEqual("Submit");
        var fields = command.GetProperty("inputs").EnumerateArray().Select(_ => (
            Name: _.GetProperty("property").GetString(), Type: _.GetProperty("type").GetString())).ToArray();
        fields.ShouldContainOnly([("name", "string"), ("projectId", "guid")]);
        command.GetProperty("inputs").EnumerateArray().All(_ => string.Join('|', _.EnumerateObject().Select(property => property.Name)) == "label|property|type").ShouldBeTrue();
        var csharp = Text(Artifact("Projects/Registration/RegisterProject/RegisterProject.cs")!);
        csharp.ShouldContain("public record RegisterProject(ProjectId ProjectId, ProjectName Name)");
        Text(Artifact("Common/ProjectId.cs")!).ShouldContain("Guid");
        Text(Artifact("Common/ProjectName.cs")!).ShouldContain("string");
    }
    [Fact] void should_bind_the_command_as_before() => Text(Artifact(SceneBindingsRenderer.RelativePath)!).ShouldContain("registerCommands({RegisterProject});");
    [Fact] void should_compose_only_the_modeled_command_and_query() => _elements.Length.ShouldEqual(2);
    [Fact] void should_have_unique_element_ids() => _elements.Select(_ => _.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count().ShouldEqual(_elements.Length);
    [Fact] void should_use_the_exact_query_binding() => _properties.GetProperty("query").GetString().ShouldEqual("ProjectById");
    [Fact] void should_show_the_actual_non_key_scalar_field() => _properties.GetProperty("resultField").GetString().ShouldEqual("name");
    [Fact] void should_declare_only_supported_component_properties() => string.Join('|', _properties.EnumerateObject().Select(_ => _.Name)).ShouldEqual("inputs|label|query|resultField|submitLabel");
    [Fact] void should_declare_only_supported_input_properties() => string.Join('|', _input.EnumerateObject().Select(_ => _.Name)).ShouldEqual("label|parameter|pattern|required|type");
    [Fact] void should_bind_the_generated_parameter_name() => _input.GetProperty("parameter").GetString().ShouldEqual("projectId");
    [Fact] void should_declare_a_string_not_a_guid_input_type() => _input.GetProperty("type").GetString().ShouldEqual("string");
    [Fact] void should_require_the_modeled_key() => _input.GetProperty("required").GetBoolean().ShouldBeTrue();
    [Fact] void should_explain_the_guid_format() => _input.GetProperty("label").GetString().ShouldContain("GUID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
    [Fact] void should_admit_upper_and_lower_case_dashed_guids() => new[] { "11223344-5566-7788-99aa-bbccddeeff00", "11223344-5566-7788-99AA-BBCCDDEEFF00" }.All(MatchesPattern).ShouldBeTrue();
    [Fact] void should_reject_malformed_nonempty_keys_before_execution() => new[] { "not-a-guid", "112233445566778899aabbccddeeff00", "{11223344-5566-7788-99aa-bbccddeeff00}", "11223344-5566-7788-99aa-bbccddeeff00\n", " 11223344-5566-7788-99aa-bbccddeeff00" }.Any(MatchesPattern).ShouldBeFalse();
    [Fact] void should_not_mutate_the_profile() => _request.Profile.Inputs.Select(_ => _.Sha256).SequenceEqual(_profileHashes).ShouldBeTrue();

    /// <summary>
    /// The frozen bytes changed because every element now carries its interactions, which serialize as an
    /// empty collection when a document attached none - the same way its slots already serialized as an empty
    /// object. The assertion below names that difference, so the new hash is explained by something readable
    /// rather than by a number somebody updated until the build went green.
    /// </summary>
    [Fact] void should_freeze_the_complete_default_scene_bytes() => Artifact(SceneCompositionInput.RelativePath)!.Sha256.ShouldEqual("4823dc11c628f99a3ead73006dc330bc9f54409b0cf05d60c58379cdc16a5396");

    [Fact] void should_declare_no_interactions_on_a_composed_element() =>
        _elements.All(element => element.GetProperty("behaviors").GetArrayLength() == 0).ShouldBeTrue();
    [Fact] void should_freeze_the_complete_binding_module_bytes() => Artifact(SceneBindingsRenderer.RelativePath)!.Sha256.ShouldEqual("4823d01d04ac795068ac52d87afc3e7be57c826dda63b005f1c3e7a057196f2f");
    [Fact] void should_repeat_the_exact_scene_bytes() => _planner.Plan(_request).Artifacts.Single(_ => _.RelativePath == SceneCompositionInput.RelativePath).Bytes.SequenceEqual(Artifact(SceneCompositionInput.RelativePath)!.Bytes).ShouldBeTrue();
    [Fact] void should_not_emit_http_literals_or_unmodeled_queries() => new[] { "http://", "https://", "/api/", "AllProjects" }.Any(value => Text(Artifact(SceneCompositionInput.RelativePath)!).Contains(value, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_emit_screen_specific_typescript() => _plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".tsx", StringComparison.Ordinal)).Select(_ => _.RelativePath).ShouldContainOnly(".frontend/main.tsx");

    bool MatchesPattern(string value) => Regex.IsMatch(value, $"^(?:{_input.GetProperty("pattern").GetString()})$", RegexOptions.None, TimeSpan.FromSeconds(1));

    PlannedArtifact? Artifact(string relativePath) => _plan.Artifacts.FirstOrDefault(_ => _.RelativePath == relativePath);
}
