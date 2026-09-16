// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_stage_application_from_a_selected_file : Specification
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
    string _file = null!;
    StageApplication _application = null!;
    EventModel _model = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage selected scene {Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _file = Path.Combine(_directory, "application.PLAY");
        File.WriteAllText(_file, Source);
        File.WriteAllText(Path.Combine(_directory, "broken.play"), "module");
        _model = EventModelLoader.LoadFromSource(Source);
    }

    async Task Because() => _application = await EventModelLoader.LoadStageApplicationFromPathAsync(_file);

    [Fact] void should_compile_only_the_selected_file_despite_an_invalid_sibling() => EventModelFile.Write(_application.EventModel).ShouldEqual(EventModelFile.Write(_model));
    [Fact] void should_translate_the_scene_from_that_same_file() => _application.Scene.Screens.Single().Name.ShouldEqual("Invoices");
    [Fact] void should_preserve_the_default_layout() => _application.Scene.Layouts.Single().Name.ShouldEqual(Scene.DefaultLayout.Name);

    void Destroy() => Directory.Delete(_directory, recursive: true);
}
