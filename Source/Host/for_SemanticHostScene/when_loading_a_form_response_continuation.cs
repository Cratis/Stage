// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Interactions;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHostScene;

public class when_loading_a_form_response_continuation : Specification
{
    string _directory = null!;
    SceneApplication _scene = null!;
    InteractionAction _continuation = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-form-response-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    void Because()
    {
        var path = Path.Combine(_directory, "Projects.play");
        File.WriteAllText(path, Source);
        _scene = SemanticHostScene.Load(path, compiled_plan.From(Source).Model);

        // The screen's action runs the command through the form, so the form's continuation travels on the action, not on an inline form.
        var action = _scene.Screens.Single(screen => screen.Name == "Overview").SlotContent.Values.SelectMany(elements => elements)
            .OfType<ExternalComponent>()
            .Single(component => component.ComponentName == "core:action" && (string)component.Properties["command"] == "RegisterProject");
        _continuation = ((IReadOnlyList<InteractionAction>)action.Properties["onSubmit"]).Single();
    }

    [Fact] void should_load_the_response_source_from_authored_presentation() => _continuation.Arguments.Single().Value.Kind.ShouldEqual(BindingSourceKind.CommandResponse);
    [Fact] void should_map_the_named_response_to_its_transport_member() => _continuation.Arguments.Single().Value.Path.ShouldEqual("registeredId");
    [Fact] void should_carry_the_response_type() => _scene.ResponseContracts["commands"]["RegisterProject"]["type"]["properties"]["registeredId"]["kind"].GetValue<string>().ShouldEqual("string");
    [Fact] void should_carry_the_destination_parameter_type() => _scene.ResponseContracts["screenInputs"]["Details"]["projectId"]["kind"].GetValue<string>().ShouldEqual("string");

    void Destroy() => Directory.Delete(_directory, recursive: true);

    const string Source = """
        concept ProjectId : Uuid
        module Projects
          form Register for RegisterProject
            on submit
              navigate to Details
                with projectId from registered_id
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId ProjectId generated identifier
                returns
                  registered_id = projectId
              screen Overview
                action RegisterProject
            slice StateView Lookup
              readmodel Project
                projectId ProjectId
              query ProjectById => Project?
                by projectId ProjectId
              screen Details
                data Project via query ProjectById by projectId
        """;
}
