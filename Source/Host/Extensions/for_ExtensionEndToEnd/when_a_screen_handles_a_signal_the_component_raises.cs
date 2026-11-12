// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Components;
using Xunit;
using AnApplication = Cratis.Stage.Host.Extensions.given.an_extension_application;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneInteractions = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Host.Extensions.for_ExtensionEndToEnd;

public class when_a_screen_handles_a_signal_the_component_raises : AnApplication
{
    SceneElements.ExternalComponent _placed = null!;
    SceneInteractions.InteractionBinding _binding = null!;
    JsonElement _trigger;

    async Task Because()
    {
        await LoadApplication();
        var catalog = new ExtensionComponentCatalog(ExtensionModelCatalogs.Placements(_uiContract, _modelPackages));
        var scene = ExtensionSceneComponents.Apply(_scene, catalog, _modelPackages);
        _placed = scene.Screens
            .SelectMany(screen => screen.SlotContent.Values)
            .SelectMany(elements => elements)
            .OfType<SceneElements.ExternalComponent>()
            .Single(component => component.ComponentName == ExtensionSceneComponents.ElementName);
        _binding = _placed.Behaviors.SelectMany(behavior => behavior.Bindings).Single();
        _trigger = JsonSerializer.SerializeToElement(_binding.Trigger, _binding.Trigger.GetType(), JsonSerializerOptions.Web).Clone();
    }

    [Fact] void should_bind_the_interaction_to_the_signal_trigger() => _binding.Trigger.ShouldBeOfExactType<SceneInteractions.SignalInteractionTrigger>();
    [Fact] void should_name_the_signal_the_contract_declares() => ((SceneInteractions.SignalInteractionTrigger)_binding.Trigger).SignalName.ShouldEqual("dismissed");
    [Fact] void should_serialize_it_as_the_signal_the_runtime_resolves() => _trigger.GetProperty("signalName").GetString().ShouldEqual("dismissed");
    [Fact] void should_run_the_command_the_screen_declares() => _binding.Actions.OfType<SceneInteractions.ExecuteCommandAction>().Single().Command.ShouldEqual("DismissNotice");
    [Fact] void should_read_the_value_the_signal_carries() => _binding.Actions.OfType<SceneInteractions.ExecuteCommandAction>().Single().Arguments.Single().Value.Path.ShouldEqual("signal.noticeId");
}
