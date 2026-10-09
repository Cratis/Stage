// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_from_a_nested_folder : given.a_source_plan
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_root, "nested"));
        File.Move(Path.Combine(_root, "projects.play"), Path.Combine(_root, "nested", "projects.play"));
    }
    async Task Because() => _result = await From(".");
    [Fact] void should_compile_sources_recursively() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_artifact_paths_application_relative() => Paths(_result).ShouldContain("Projects/Registration/RegisterProject/RegisterProject.cs");
}
