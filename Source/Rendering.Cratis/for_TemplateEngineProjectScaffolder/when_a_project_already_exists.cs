// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_TemplateEngineProjectScaffolder;

public class when_a_project_already_exists : Specification
{
    DirectoryInfo _target = null!;
    TemplateEngineProjectScaffolder _scaffolder = null!;
    bool _result;

    void Establish()
    {
        _target = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"stage-scaffold-existing-{Guid.NewGuid():N}"));
        _target.Create();
        File.WriteAllText(Path.Combine(_target.FullName, "Existing.csproj"), "<Project />");
        _scaffolder = new TemplateEngineProjectScaffolder();
    }

    async Task Because() => _result = await _scaffolder.EnsureScaffolded(_target, TextWriter.Null);

    [Fact] void should_not_scaffold() => _result.ShouldBeFalse();

    void Destroy() => _target.Delete(recursive: true);
}
