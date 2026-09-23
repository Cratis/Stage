// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

// Real emitted C# builds run the pinned Arc proxy generator. No npm install, local proxy patch or HTTP stub.
public class when_matching_lookup_inputs_to_native_proxies(when_matching_lookup_inputs_to_native_proxies.guid_context identifier, when_matching_lookup_inputs_to_native_proxies.string_context text)
    : IClassFixture<when_matching_lookup_inputs_to_native_proxies.guid_context>, IClassFixture<when_matching_lookup_inputs_to_native_proxies.string_context>
{
    [Fact] void should_build_the_guid_backend_without_warnings() => identifier.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_build_the_string_backend_without_warnings() => text.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_emit_the_actual_guid_descriptor_parameter() => identifier.Proxy.ShouldContain($"new ParameterDescriptor('{identifier.Parameter}', Guid, false)");
    [Fact] void should_emit_the_actual_string_descriptor_parameter() => text.Proxy.ShouldContain($"new ParameterDescriptor('{text.Parameter}', String, false)");
    [Fact] void should_use_the_canonical_parameter_in_both_variants() => new[] { identifier.Parameter, text.Parameter }.ShouldContainOnly("projectId", "projectId");
    [Fact] void should_preserve_optional_single_proxy_semantics() => new[] { identifier.Proxy, text.Proxy }.All(_ => _.Contains("super(ProjectSummary, false)", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_display_an_own_native_string_property() => new[] { identifier.Proxy, text.Proxy }.All(_ => _.Contains("name!: string", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_add_a_format_or_default_to_string_keys() => text.InputProperties.ShouldEqual("label|parameter|required|type");
    [Fact] void should_emit_the_requested_scene_package_not_a_substitute() => new[] { identifier.SceneVersion, text.SceneVersion }.ShouldContainOnly("3.7.0", "3.7.0");

    public class guid_context : native_context;
    public class string_context : native_context
    {
        protected override bool StringKey => true;
    }

    public class native_context : a_generated_application
    {
        public string Warnings { get; private set; } = null!;
        public string Proxy { get; private set; } = null!;
        public string Parameter { get; private set; } = null!;
        public string InputProperties { get; private set; } = null!;
        public string SceneVersion { get; private set; } = null!;
        protected virtual bool StringKey => false;

        protected override ArtifactRenderPlan CreatePlan()
        {
            if (StringKey)
            {
                var form = Corpus.SourceForms.Single(_ => _.Name == "single");
                var source = form.Documents.Single();
                _model = Compile(form with { Documents = [source with { Bytes = [.. Encoding.UTF8.GetBytes(source.Text.Replace("concept ProjectId : Uuid", "concept ProjectId : String", StringComparison.Ordinal))] }] }).Model;
                _executionPlan = SemanticExecutionPlan.Compile(_model).Plan!;
            }

            return CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), _options);
        }

        async Task Because()
        {
            try
            {
                Warnings = BuildWarnings(await Run("lookup-native-build.log", "build", "Projects.csproj", "--nologo"));
                Proxy = ReadGeneratedFile("Projects/Registration/ProjectLookup/ProjectLookup.ts");
                using var scene = JsonDocument.Parse(ReadGeneratedFile("scene.json"));
                var properties = scene.RootElement.GetProperty("screens")[0].GetProperty("slotContent").GetProperty(DefaultLayout.ContentSlotName)
                    .EnumerateArray().Single(_ => _.GetProperty("componentName").GetString() == "Cratis.Components:queryInputForm").GetProperty("properties");
                var input = properties.GetProperty("inputs")[0];
                Parameter = input.GetProperty("parameter").GetString()!;
                InputProperties = string.Join('|', input.EnumerateObject().Select(_ => _.Name));
                properties.GetProperty("resultField").GetString().ShouldEqual("name");
                using var package = JsonDocument.Parse(ReadGeneratedFile("package.json"));
                SceneVersion = package.RootElement.GetProperty("dependencies").GetProperty("@cratis/scene.components").GetString()!;
            }
            finally
            {
                Cleanup();
            }
        }
    }
}
#endif
