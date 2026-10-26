// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names.given;

public class a_two_module_request : a_register_project_render_request
{
    protected static readonly string[] UserInterfacePaths = [SceneCompositionInput.RelativePath, SceneBindingsRenderer.RelativePath, StageSceneDocument.RelativePath, StageModuleRenderer.RelativePath];

    protected ApplicationSyntax _syntax = null!;

    void Establish()
    {
        var form = Corpus.SourceForms.Single(_ => _.Name == "single");
        var original = form.Documents.Single();
        const string AdditionalModule = """
            module OtherProjects
              feature Registration
                slice StateChange OtherRegistration
                  command OtherRegisterProject
                    projectId ProjectId identifier
                    produces OtherProjectRegistered
                      for projectId
                      projectId = projectId
                  event OtherProjectRegistered
                    projectId ProjectId
            """;
        var source = original.Text + Environment.NewLine + AdditionalModule;
        _syntax = new ScreenplayCompiler().Compile(source).Value!;
        _model = Compile(form with { Documents = [original with { Bytes = [.. Encoding.UTF8.GetBytes(source)] }] }).Model;
        UpdateRequest();
    }

    protected void DuplicateCommandName()
    {
        // As with same-named queries, mutate distinct validated sources without relying on textual name binding.
        var modules = _model.Application.Modules.Select(module => module.Name == "OtherProjects"
            ? module with
            {
                Features = [.. module.Features.Select(feature => feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Commands = [.. slice.Commands.Select(command => command with { Name = "RegisterProject" })]
                })]
            })]
            }
            : module);
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Modules = [.. modules] });
        _syntax = _syntax with
        {
            Modules = [.. _syntax.Modules.Select(module => module.Name == "OtherProjects" ? module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Commands = [.. slice.Commands.Select(command => command with { Name = "RegisterProject" })]
                    })]
                })]
            } : module)]
        };
        UpdateRequest();
    }

    protected void AddSecondSharedCommand(bool withSelectableQueries = false)
    {
        const string command = """
              command ArchiveProject
                projectId ProjectId identifier
                archiveReason String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = archiveReason
        """;
        var source = Source.Replace("      event ProjectRegistered", command + "\n      event ProjectRegistered", StringComparison.Ordinal);
        if (withSelectableQueries)
        {
            source = source.Replace("      query ProjectById", "      query AllProjects => observable ProjectSummary[]\n      query ProjectById", StringComparison.Ordinal);
        }
        var other = source.Replace("Project", "OtherProject", StringComparison.Ordinal);
        var first = invoice_model.Compile(source);
        var second = invoice_model.Compile(other);
        _model = ExecutableSemanticModel.Create(first.LanguageVersion, first.SemanticVersion, first.Application with
        {
            Concepts = [.. first.Application.Concepts, .. second.Application.Concepts],
            Modules = [.. first.Application.Modules, .. second.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Commands = [.. slice.Commands.Select(command => command with { Name = command.Name.Replace("OtherProject", "Project", StringComparison.Ordinal) })]
                    })]
                })]
            })]
        });
        _syntax = new ScreenplayCompiler().Compile(source + "\n" + other).Value!;
        _syntax = _syntax with
        {
            Modules = [.. _syntax.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Commands = [.. slice.Commands.Select(command => command with { Name = command.Name.Replace("OtherProject", "Project", StringComparison.Ordinal) })]
                    })]
                })]
            })]
        };
        UpdateRequest();
    }

    protected void CarryAuthoredScene(bool qualified, bool includeToolbar = false, string? commandComponent = null)
    {
        var context = new SemanticApplicationContext(_request, _options);
        var scene = DefaultSceneComposition.Create(context)! with
        {
            Structure = StructureConverter.Convert(_syntax)
        };
        var document = JsonNode.Parse(CanonicalSceneJson.Serialize(scene))!.AsObject();
        document["structure"] = CanonicalSceneJson.ToNode(scene.Structure);
        if (includeToolbar)
        {
            document["screens"]![0]!["slotContent"]![DefaultLayout.ContentSlotName]!.AsArray().Insert(0, CanonicalSceneJson.ToNode(new global::Cratis.Scene.Model.Elements.ExternalComponent
            {
                Id = "register-project-toolbar",
                Name = "Register project",
                ComponentName = "core:toolbarItem",
                Properties = new Dictionary<string, object?>
                {
                    ["label"] = "Register project",
                    ["slice"] = document["structure"]!["modules"]![0]!["features"]![0]!["slices"]![0]!["id"]!.GetValue<string>()
                }
            }));
        }
        var json = CanonicalSceneJson.Write(document);
        if (commandComponent is not null)
        {
            json = json.Replace("\"componentName\":\"Cratis.Components:commandForm\"", $"\"componentName\":\"{commandComponent}\"", StringComparison.Ordinal);
        }
        if (!qualified)
        {
            foreach (var slice in context.AllSlices)
            {
                foreach (var command in slice.Slice.Commands)
                {
                    json = json.Replace($"\"{context.SceneCommands.ById[command.Id].Key}\"", $"\"{command.Name}\"", StringComparison.Ordinal);
                }
            }
        }

        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options, scene);
        var composed = CratisArtifactRenderInput.CreateText(SceneCompositionInput.RelativePath, CratisRendering.TargetVersion, json);
        _request = _request with
        {
            Profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(input => input.Name != composed.Name), composed])
        };
    }

    protected void UpdateRequest()
    {
        _executionPlan = SemanticExecutionPlan.Compile(_model).Plan!;
        _request = new(_model, _executionPlan, _request.Profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id));
    }
}
