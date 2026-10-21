// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

/// <summary>
/// Studio exports where its canvases place event sources and the names of its seed data collections. Both are authoring
/// metadata, and Play must still load the model - a parser without them refused it and Play answered 404.
/// </summary>
public class when_loading_studio_canvas_metadata : Specification
{
    string _folder = null!;
    SemanticModelLoadResult _result = null!;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"stage-semantic-loader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "model.play"),
            """
            concept ProjectId : Uuid
            eventsource Project
              description "One project"
              identifier ProjectId
              position 110 150
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    name String
                    produces ProjectRegistered
                      name = name
                  event ProjectRegistered
                    name String
            seed "Starter projects"
              for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e1001"
                ProjectRegistered
                  name = "Hermes"
            """);
    }

    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_folder, ["model.play"], null, "Portfolio");

    [Fact] void should_load_the_model() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _result.Errors.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldBeEmpty();

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
#endif
