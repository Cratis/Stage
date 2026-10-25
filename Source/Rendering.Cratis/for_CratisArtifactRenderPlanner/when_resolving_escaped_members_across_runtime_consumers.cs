// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_resolving_escaped_members_across_runtime_consumers : a_generated_application
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    ArtifactRenderPlan _plan = null!;
    Assembly _assembly = null!;
    Type _command = null!;
    IAuthorizationPolicy _policy = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var model = invoice_model.Compile(escaped_member_application.Source);
        var profile = CratisRendering.CreateProfile(model.Application.Name, new("Escapes", "Escapes"), escaped_member_application.Scene());
        _plan = new CratisArtifactRenderPlanner().Plan(new(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        var sources = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        _assembly = RenderedOutput.Load(sources);
        _command = _assembly.GetTypes().Single(type => type.Name == "Text");
        var command = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single();
        _policy = (IAuthorizationPolicy)Activator.CreateInstance(_assembly.GetTypes().Single(type => type.Name == policy_names.ClassOf(model, command.Id)))!;
        return _plan;
    }

    async Task Because()
    {
        BuildWarnings(await Run("escaped-build.log", "build", "-c", "Debug", "-warnaserror")).ShouldEqual(string.Empty);
        (await Run("escaped-tests.log", "test", "Escapes.csproj", "-c", "Debug", "--no-build")).ShouldHavePassedSpecifications();
    }

    [Fact]
    async Task should_use_the_same_escaped_contract_for_every_generated_consumer()
    {
        var instance = JsonSerializer.Deserialize("""{"text_":"one","handle_":"payload"}""", _command, JsonOptions)!;
        _command.GetProperty("Text_")!.GetValue(instance).ShouldEqual("one");
        _command.GetProperty("Handle_")!.GetValue(instance).ShouldEqual("payload");
        var resource = new CommandContext(CorrelationId.New(), _command, instance, [], CommandContextValues.Empty);
        (await Allows(resource, "one", "one")).ShouldBeTrue();
        (await Allows(resource, "two", "one")).ShouldBeFalse();
        (await Allows(resource, "one", "two")).ShouldBeFalse();

        var runtime = JsonNode.Parse(Artifact(StageSceneDocument.RelativePath))!.AsObject();
        var schema = JsonNode.Parse(runtime["commandSchemas"]!["Text"]!.GetValue<string>())!;
        schema["properties"]!.AsObject().Select(pair => pair.Key).ShouldContainOnly("text_", "handle_");
        schema["required"]!.AsArray().Select(value => value!.GetValue<string>()).ShouldContainOnly("text_", "handle_");
        var screen = runtime["screens"]!.AsArray().Single()!;
        var form = screen["slotContent"]!["forms"]!.AsArray().Single()!["properties"]!;
        form["fields"]!.AsArray().Select(field => field!["name"]!.GetValue<string>()).ShouldContainOnly("text_", "valueInput");
        Assert.Null(form["fields"]![0]!["sourceProperty"]);
        form["fields"]![1]!["sourceProperty"]!.GetValue<string>().ShouldEqual("handle_");
        form["layout"]!["placements"]![0]!["field"]!.GetValue<string>().ShouldEqual("valueInput");
        var action = screen["slotContent"]![DefaultLayout.ContentSlotName]!.AsArray()[0]!["properties"]!;
        action["arguments"]!.AsObject().Single().Key.ShouldEqual("handle_");
        action["fields"]!.AsArray().Select(field => (field!["sourceProperty"] ?? field["name"])!.GetValue<string>()).ShouldContainOnly("text_", "handle_");
        Assert.Null(action["fields"]![0]!["sourceProperty"]);
        var toolbar = screen["slotContent"]![DefaultLayout.ContentSlotName]!.AsArray()[1]!;
        toolbar["slots"]!["items"]![0]!["properties"]!["slice"]!.GetValue<string>().ShouldEqual("FollowUp");

        // The live semantic Host still accepts the source contract; it must not inherit generated CLR escapes.
        var semantic = invoice_model.Compile(escaped_member_application.Source);
        var properties = semantic.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single().Properties;
        JsonNode.Parse(CommandFormSchemas.For(semantic.Application, properties))!["properties"]!.AsObject().Select(pair => pair.Key).ShouldContainOnly("text", "handle");

        var query = _assembly.GetTypes().Single(type => type.Name == "Details").GetMethod("Details_")!;
        query.GetCustomAttribute<global::Cratis.Arc.Queries.ModelBound.PathAttribute>()!.Path.ShouldEqual("/api/work/processing/lookup/details");
        Artifact(SceneBindingsRenderer.RelativePath).ShouldContain("import { Details_ as __sceneQuery0 }");
        Artifact(SceneBindingsRenderer.RelativePath).ShouldContain("import { Details_ as __sceneQuery0 }");
        var proxy = GeneratedFiles("*.ts").Select(ReadGeneratedFile).Single(content => content.Contains("export class Text ", StringComparison.Ordinal));
        proxy.ShouldContain("text_");
        proxy.ShouldContain("handle_");
        var reaction = ReadGeneratedFile("Work/Processing/FollowUp/when_completing.cs");
        reaction.ShouldContain("command.Text_");
        reaction.ShouldContain("command.Handle_");
        reaction.ShouldContain("should_invoke_the_expected_command");
    }

    async Task<bool> Allows(object resource, string owner, string target)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new("owner", owner), new("target", target)], "fixture"));
        return await _policy.IsAuthorized(new(principal, _command, resource), CancellationToken.None);
    }

    string Artifact(string path) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());
}
