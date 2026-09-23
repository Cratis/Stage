// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_Samples;

/// <summary>
/// The sample application in Samples/Billing is the one document in this repository that exercises the
/// language broadly rather than one construct at a time, so it is the only place a gap between what Screenplay
/// admits and what Stage can translate shows up before a user finds it.
/// </summary>
/// <remarks>
/// It has already earned that: it found a valid <c language="csharp">target size expanded</c> crashing the translation, and
/// behaviors written inside a filled slot being dropped without a word. Compiling it here means neither can
/// come back quietly, and that the sample cannot rot into something that no longer compiles.
/// </remarks>
public class when_translating_the_billing_sample : Specification
{
    ApplicationCompilation<SceneApplication> _compilation = null!;
    ScreenplaySceneVisitor _visitor = null!;

    static string SamplePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Samples")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "Samples", "Billing");
    }

    void Because()
    {
        _visitor = new ScreenplaySceneVisitor();
        _compilation = new PlayFileCompiler().CompileFolder(SamplePath(), _visitor);
    }

    [Fact] void should_compile_without_errors() =>
        _compilation.Result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

    [Fact] void should_compile_without_warnings() =>
        _compilation.Result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Warning).ShouldBeEmpty();

    [Fact] void should_translate_without_findings() => _visitor.Findings.ShouldBeEmpty();

    [Fact] void should_read_every_file_of_the_sample() => _compilation.Sources.Count().ShouldEqual(14);

    [Fact] void should_translate_every_screen() => _compilation.Result.Value!.Screens.Count.ShouldEqual(4);

    [Fact] void should_translate_the_shell() => _compilation.Result.Value!.Layouts.Count.ShouldEqual(1);

    [Fact] void should_translate_a_profile_per_platform() => _compilation.Result.Value!.UiProfiles.Count.ShouldEqual(4);

    [Fact] void should_translate_both_templates() =>
        (_compilation.Result.Value!.ScreenTemplates.Count + _compilation.Result.Value!.DialogTemplates.Count).ShouldEqual(2);

    // The interaction is the part with the furthest to travel: declared in one file, attached in another,
    // through a template slot, and it still arrives.
    [Fact] void should_attach_the_behaviors_the_screens_use() =>
        _compilation.Result.Value!.Screens.Sum(screen => screen.Behaviors.Count).ShouldEqual(4);
}
