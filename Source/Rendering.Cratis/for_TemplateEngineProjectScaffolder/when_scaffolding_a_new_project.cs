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
        _result = await _scaffolder.EnsureScaffolded(_target, "AcmeBilling", writer);
        _output = writer.ToString();
    }

    [Fact] void should_scaffold() => _result.ShouldBeTrue();
    [Fact] void should_report_progress() => _output.ShouldContain("Scaffolding complete.");
    [Fact] void should_generate_a_project_file_named_after_the_project() =>
        File.Exists(Path.Combine(_target.FullName, "AcmeBilling.csproj")).ShouldBeTrue();
    [Fact] void should_substitute_the_target_framework() =>
        File.ReadAllText(Path.Combine(_target.FullName, "AcmeBilling.csproj")).ShouldNotContain("TARGET_FRAMEWORK");
    [Fact] void should_substitute_the_root_namespace() =>
        File.ReadAllText(Path.Combine(_target.FullName, "AcmeBilling.csproj")).ShouldContain("<RootNamespace>AcmeBilling</RootNamespace>");
    [Fact] void should_substitute_the_project_guid_in_the_solution() =>
        File.ReadAllText(Path.Combine(_target.FullName, "AcmeBilling.sln")).ShouldNotContain("PROJECT_GUID");
    [Fact] void should_reference_the_generated_project_from_the_solution() =>
        File.ReadAllText(Path.Combine(_target.FullName, "AcmeBilling.sln")).ShouldContain("AcmeBilling.csproj");
    [Fact] void should_remove_the_sample_slice() =>
        Directory.Exists(Path.Combine(_target.FullName, "SomeModule")).ShouldBeFalse();
    [Fact] void should_remove_the_sample_slice_from_the_composition() =>
        File.ReadAllText(Path.Combine(_target.FullName, "App.tsx")).ShouldNotContain("SomeFeature");

    void Destroy()
    {
        if (_target.Exists)
        {
            _target.Delete(recursive: true);
        }
    }
}
