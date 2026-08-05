// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_TemplateEngineProjectScaffolder;

public class when_scaffolding_a_new_project : Specification
{
    DirectoryInfo _target = null!;
    TemplateEngineProjectScaffolder _scaffolder = null!;
    bool _result;
    string _output = null!;

    void Establish()
    {
        _target = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"stage-scaffold-new-{Guid.NewGuid():N}"));
        _scaffolder = new TemplateEngineProjectScaffolder();
    }

    async Task Because()
    {
        var writer = new StringWriter();
        _result = await _scaffolder.EnsureScaffolded(_target, writer);
        _output = writer.ToString();
    }

    [Fact] void should_scaffold() => _result.ShouldBeTrue();
    [Fact] void should_report_progress() => _output.ShouldContain("Scaffolding complete.");
    [Fact] void should_generate_a_project_file() => _target.EnumerateFiles("*.csproj").Any().ShouldBeTrue();

    void Destroy()
    {
        if (_target.Exists)
        {
            _target.Delete(recursive: true);
        }
    }
}
