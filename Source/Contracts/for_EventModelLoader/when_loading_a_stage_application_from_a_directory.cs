// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_stage_application_from_a_directory : Specification
{
    const string Source =
        """
        module Sales
          feature Invoices
            slice StateChange Create
              command CreateInvoice
                id Uuid
              screen Invoices
                title "Invoices"
        """;

    string _directory = null!;
    StageApplication _application = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-specs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "application.play"), Source);
    }

    async Task Because() => _application = await EventModelLoader.LoadStageApplicationFromDirectoryAsync(_directory);

    [Fact] void should_translate_the_event_model() => _application.EventModel.Collections.Single().Modules.Single().Name.ShouldEqual("Sales");
    [Fact] void should_translate_the_scene_from_the_same_application() => _application.Scene.Layouts.Single().Name.ShouldEqual(Scene.DefaultLayout.Name);
    [Fact] void should_translate_the_screen_from_the_same_application() => _application.Scene.Screens.Single().Name.ShouldEqual("Invoices");

    void Destroy()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
