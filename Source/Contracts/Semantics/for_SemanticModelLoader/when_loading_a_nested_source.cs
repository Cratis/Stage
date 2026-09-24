// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_nested_source : Specification
{
    string _folder = null!;
    LoadedSemanticModel _loaded = null!;
    LoadedSemanticModel _named = null!;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"stage-semantic-loader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_folder, "Projects"));
        File.WriteAllText(Path.Combine(_folder, "Projects", "Register.play"),
            """
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    produces ProjectRegistered
                      projectId = projectId
                  event ProjectRegistered
                    projectId ProjectId
            """);
    }

    async Task Because()
    {
        _loaded = await SemanticModelLoader.LoadFromPathAsync(_folder);
        _named = await SemanticModelLoader.LoadFromPathAsync(_folder, catalogPath: null, applicationName: "Portfolio");
    }

    [Fact] void should_compile_an_executable_plan() => _loaded.Plan.Commands.Count.ShouldEqual(1);
    [Fact] void should_keep_the_application_revision() => _loaded.Plan.Revision.ShouldEqual(_loaded.Model.Revision);
    [Fact] void should_use_the_optional_application_name() => _named.Model.Application.Name.ShouldEqual("Portfolio");

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
#endif
