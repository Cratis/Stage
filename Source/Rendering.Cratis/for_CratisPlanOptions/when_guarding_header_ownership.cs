// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions;

public class when_guarding_header_ownership : Specification
{
    [Fact] void should_have_no_target_header_writer() => typeof(CratisArtifactRenderPlanner).Assembly.GetTypes().Any(type => type.Name == "GeneratedFileHeader").ShouldBeFalse();
    [Fact] void should_keep_the_legacy_input_free_of_artifact_writers() => typeof(FileHeaderInput).GetMethods().Any(method => method.ReturnType == typeof(PlannedArtifact)).ShouldBeFalse();

    [Fact] void should_apply_headers_only_in_the_scaffold_pipeline()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Stage.slnx"))) directory = directory.Parent;
        directory.ShouldNotBeNull();
        var roots = new[] { "Rendering.Cratis", "Rendering.Cratis.Scaffolding" }.Select(name => Path.Combine(directory!.FullName, "Source", name)).Where(Directory.Exists).ToArray();
        roots.ShouldNotBeEmpty();
        var sources = roots.SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(segment => segment.StartsWith("for_", StringComparison.Ordinal) || segment == "given" || segment == "obj" || segment == "bin"))
            .Select(path => (Path: path, Text: File.ReadAllText(path))).ToArray();
        sources.ShouldNotBeEmpty();
        sources.Where(source => source.Text.Contains("ArtifactFileHeader.Apply(", StringComparison.Ordinal)).Select(source => Path.GetFileName(source.Path)).ShouldEqual(["CratisRendering.SourcePlans.cs"]);
        sources.Any(source => source.Text.Contains("ArtifactFileHeader.Comment(", StringComparison.Ordinal)).ShouldBeFalse();
    }
}
