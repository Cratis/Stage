// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_DefaultSceneComposition;

// These isolated composition policies do not broaden backend admission. Planner admission runs first.
public class when_selecting_editable_lookups : a_register_project_render_request
{
    [Theory]
    [InlineData(SemanticPrimitiveType.Text)]
    [InlineData(SemanticPrimitiveType.Uuid)]
    void should_accept_only_native_string_or_guid_scalar_keys(SemanticPrimitiveType primitive) =>
        Elements(Context(query: Key(primitive))).Count(IsLookup).ShouldEqual(1);

    [Theory]
    [InlineData(SemanticPrimitiveType.WholeNumber)]
    [InlineData(SemanticPrimitiveType.DecimalNumber)]
    [InlineData(SemanticPrimitiveType.Boolean)]
    [InlineData(SemanticPrimitiveType.Date)]
    [InlineData(SemanticPrimitiveType.DateTime)]
    void should_omit_unsupported_key_descriptors(SemanticPrimitiveType primitive) =>
        Elements(Context(query: Key(primitive))).Any(IsLookup).ShouldBeFalse();

    [Fact] void should_reject_collection_keys_before_composition() => Catch.Exception(() => Context(query: Key(SemanticPrimitiveType.Text, collection: true))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_unknown_keys_before_composition() => Catch.Exception(() => Context(query: Key(SemanticPrimitiveType.Unknown))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_leave_string_keys_unformatted_and_without_defaults() => string.Join('|', Input(Context(query: Key(SemanticPrimitiveType.Text))).EnumerateObject().Select(_ => _.Name)).ShouldEqual("label|parameter|required|type");
    [Fact] void should_reject_optional_identifiers_before_composition() => Catch.Exception(() => Context(query: Key(SemanticPrimitiveType.Text, optional: true))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_not_escape_a_keyword_parameter_as_a_csharp_identifier() => Input(Context(query: Query with { Argument = Query.Argument with { Name = "class" } })).GetProperty("parameter").GetString().ShouldEqual("class");
    [Fact] void should_convert_the_actual_argument_name_not_infer_it_from_the_model() => Input(Context(query: Query with { Argument = Query.Argument with { Name = "Lookup_Key" } })).GetProperty("parameter").GetString().ShouldEqual("lookupKey");

    [Theory]
    [InlineData(SemanticPrimitiveType.Text)]
    [InlineData(SemanticPrimitiveType.WholeNumber)]
    [InlineData(SemanticPrimitiveType.DecimalNumber)]
    [InlineData(SemanticPrimitiveType.Boolean)]
    void should_accept_native_scalar_result_fields(SemanticPrimitiveType primitive) =>
        Elements(Context(result: Result(SemanticTypeReference.ForPrimitive(primitive)))).Single(IsLookup).GetProperty("properties").GetProperty("resultField").GetString().ShouldEqual("name");

    [Theory]
    [InlineData(SemanticPrimitiveType.Uuid)]
    [InlineData(SemanticPrimitiveType.Date)]
    [InlineData(SemanticPrimitiveType.DateTime)]
    void should_omit_non_renderable_result_fields(SemanticPrimitiveType primitive) =>
        Elements(Context(result: Result(SemanticTypeReference.ForPrimitive(primitive)))).Any(IsLookup).ShouldBeFalse();

    [Fact] void should_omit_collection_result_fields() => Elements(Context(result: Result(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) with { IsCollection = true }))).Any(IsLookup).ShouldBeFalse();
    [Fact] void should_omit_optional_result_fields_that_can_be_null() => Elements(Context(result: Result(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) with { IsOptional = true }))).Any(IsLookup).ShouldBeFalse();
    [Fact] void should_preserve_arc_acronym_member_names() => Elements(Context(result: NonKey with { Name = "URL" })).Single(IsLookup).GetProperty("properties").GetProperty("resultField").GetString().ShouldEqual("URL");
    [Fact] void should_not_fabricate_a_result_when_only_the_key_exists() => Elements(Context(onlyKey: true)).Any(IsLookup).ShouldBeFalse();
    [Fact] void should_not_fabricate_a_query_when_none_is_modeled() => Elements(Context(noQuery: true)).Any(IsLookup).ShouldBeFalse();
    [Fact] void should_return_no_composition_when_there_are_no_commands_or_queries() => DefaultSceneComposition.Create(Context(noQuery: true, noCommands: true)).ShouldBeNull();
    [Fact] void should_return_no_composition_when_nothing_is_renderable() => DefaultSceneComposition.Create(Context(onlyKey: true, noCommands: true)).ShouldBeNull();
    [Fact] void should_compose_a_query_without_requiring_a_command_form() => Elements(Context(noCommands: true)).Single().GetProperty("componentName").GetString().ShouldEqual("Cratis.Components:queryInputForm");
    [Fact] void should_not_collide_with_a_command_having_the_same_name() => Elements(Context(query: Query with { Name = _registerProject.Commands.Single().Name })).Select(_ => _.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count().ShouldEqual(2);
    [Fact] void should_alias_a_query_import_that_has_the_same_export_name_as_a_command() => SceneBindingsRenderer.Render(Context(query: Query with { Name = _registerProject.Commands.Single().Name })).ShouldContain("import { RegisterProject as __sceneQuery0 } from '../Projects/Registration/ProjectLookup';");
    [Fact] void should_import_the_actual_generated_pascal_case_query_export() => SceneBindingsRenderer.Render(Context(query: Query with { Name = "find_project" })).ShouldContain("import { FindProject as __sceneQuery0 }");
    [Fact] void should_register_the_exact_semantic_name_not_the_normalized_export() => SceneBindingsRenderer.Render(Context(query: Query with { Name = "find_project" })).ShouldContain($"registerQueryIdentity(\"find_project\", \"{Query.Id}\", __sceneQuery0);");
    [Fact] void should_be_independent_of_property_enumeration_order_with_multiple_eligible_fields() => CanonicalSceneJson.Serialize(DefaultSceneComposition.Create(Context(reverse: true, additional: true))!).ShouldEqual(CanonicalSceneJson.Serialize(DefaultSceneComposition.Create(Context(additional: true))!));
    [Fact] void should_skip_an_unsupported_field_in_favor_of_an_actual_scalar() => Elements(Context(result: Result(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid)), additional: true)).Single(IsLookup).GetProperty("properties").GetProperty("resultField").GetString().ShouldEqual("displayName");

    [Fact] void should_render_both_canonical_command_properties_as_explicit_native_inputs()
    {
        var form = Command(Context()).GetProperty("properties");
        form.GetProperty("command").GetString().ShouldEqual("RegisterProject");
        form.GetProperty("submitLabel").GetString().ShouldEqual("Submit");
        var inputs = form.GetProperty("inputs").EnumerateArray().ToArray();
        inputs.Select(_ => _.GetProperty("property").GetString()).ShouldContainOnly(["name", "projectId"]);
        inputs.Single(_ => _.GetProperty("property").GetString() == "projectId").GetProperty("type").GetString().ShouldEqual("guid");
        inputs.Single(_ => _.GetProperty("property").GetString() == "name").GetProperty("type").GetString().ShouldEqual("string");
        inputs.All(_ => string.Join('|', _.EnumerateObject().Select(property => property.Name)) == "label|property|type").ShouldBeTrue();
        inputs.Single(_ => _.GetProperty("property").GetString() == "projectId").GetProperty("label").GetString().ShouldEqual("Project ID");
    }

    [Fact] void should_show_a_diagnostic_instead_of_a_partial_form_for_an_unsupported_required_property()
    {
        var element = Command(Context(commandProperties: [.. CommandProperties, CommandProperties[0] with
        {
            Id = SemanticId.Create(SemanticAddress.ForApplication(ApplicationIdentity.Create("UnsupportedCommandProperty"))),
            Name = "details",
            Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) with { IsCollection = true }
        }]));
        element.GetProperty("componentName").GetString().ShouldEqual("core:text");
        element.GetProperty("properties").GetProperty("text").GetString().ShouldContain("details");
        element.GetProperty("properties").TryGetProperty("inputs", out _).ShouldBeFalse();
    }

    [Fact] void should_reject_proxy_property_collisions_instead_of_binding_two_values_to_one_input()
    {
        var element = Command(Context(commandProperties: [.. CommandProperties, CommandProperties[0] with
        {
            Id = SemanticId.Create(SemanticAddress.ForApplication(ApplicationIdentity.Create("CollidingCommandProperty"))),
            Name = "project_id"
        }]));
        element.GetProperty("componentName").GetString().ShouldEqual("core:text");
        element.GetProperty("properties").GetProperty("text").GetString().ShouldContain("collision 'projectId'");
    }

    SemanticProperty[] CommandProperties => [.. _registerProject.Commands.Single().Properties];
    static JsonElement Command(SemanticApplicationContext context) => Elements(context).Single(_ => _.GetProperty("id").GetString() == "RegisterProject");

    SemanticKeyedQuery Query => _projectLookup.Queries.Single();
    SemanticProperty NonKey => _projectLookup.ReadModels.Single().Properties.Single(_ => _.Id != Query.KeyProperty);

    SemanticKeyedQuery Key(SemanticPrimitiveType primitive, bool collection = false, bool optional = false) => Query with
    {
        Argument = Query.Argument with { Type = SemanticTypeReference.ForPrimitive(primitive) with { IsCollection = collection, IsOptional = optional } }
    };

    SemanticProperty Result(SemanticTypeReference type) => NonKey with { Type = type };

    SemanticApplicationContext Context(SemanticKeyedQuery? query = null, SemanticProperty? result = null, bool onlyKey = false, bool noQuery = false, bool noCommands = false, bool reverse = false, bool additional = false, SemanticProperty[]? commandProperties = null)
    {
        var readModel = _projectLookup.ReadModels.Single();
        var properties = readModel.Properties.Where(_ => !onlyKey || _.Id == Query.KeyProperty)
            .Select(_ => _.Id == NonKey.Id ? result ?? _ : _ with { Type = (query ?? Query).Argument.Type });
        if (additional)
        {
            properties = properties.Append(NonKey with
            {
                Id = SemanticId.Create(SemanticAddress.ForApplication(ApplicationIdentity.Create("AdditionalResultField"))),
                Name = "displayName"
            });
        }

        var lookup = _projectLookup with
        {
            Projections = [],
            Specifications = [],
            Queries = noQuery ? [] : [query ?? Query],
            ReadModels = [readModel with { Properties = [.. reverse ? properties.Reverse() : properties] }]
        };
        var registration = _registerProject with { Commands = noCommands ? [] : [.. _registerProject.Commands.Select(_ => _ with { Properties = commandProperties is null ? _.Properties : [.. commandProperties] })], Specifications = [] };
        var feature = _feature with { Slices = [registration, lookup] };
        var application = _model.Application with { Modules = [_module with { Features = [feature] }] };
        var request = new ArtifactRenderRequest(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application), _executionPlan, _request.Profile, _request.Scope);
        return new(request, _options);
    }

    static bool IsLookup(JsonElement element) => element.GetProperty("componentName").GetString() == "Cratis.Components:queryInputForm";

    static JsonElement Input(SemanticApplicationContext context) => Elements(context).Single(IsLookup).GetProperty("properties").GetProperty("inputs")[0];

    static JsonElement[] Elements(SemanticApplicationContext context)
    {
        using var document = JsonDocument.Parse(CanonicalSceneJson.Serialize(DefaultSceneComposition.Create(context)!));
        return [.. document.RootElement.GetProperty("screens")[0].GetProperty("slotContent").GetProperty(DefaultLayout.ContentSlotName).EnumerateArray().Select(_ => _.Clone())];
    }
}
