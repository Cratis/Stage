// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ServiceLayout;

public class when_applying_the_service_layout : Specification
{
    ArtifactRenderProfile _default = null!;
    ArtifactRenderProfile _layout = null!;
    PlannedArtifact[] _artifacts = null!;
    PlannedArtifact[] _unchanged = null!;
    PlannedArtifact[] _moved = null!;

    void Because()
    {
        _default = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"));
        _layout = CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { ServiceLayout = true });
        _artifacts =
        [
            PlannedArtifact.CreateText("Program.cs", "var a = 1;\n"),
            PlannedArtifact.CreateText("Projects.csproj", "<Project />\n"),
            PlannedArtifact.CreateText("Projects.slnx", "<Solution />\n"),
            PlannedArtifact.CreateText(".editorconfig", "root = true\n"),
            PlannedArtifact.CreateText("Directory.Build.props", "<Project />\n"),
            PlannedArtifact.CreateText("Directory.Build.targets", "<Project />\n"),
            PlannedArtifact.CreateText("Directory.Packages.props", "<Project />\n"),
            PlannedArtifact.CreateText("docker-compose.yml", "services:\n"),
            PlannedArtifact.CreateText("Orders/Directory.Build.props", "<Project />\n"),
            PlannedArtifact.CreateText("Orders/docker-compose.yml", "services:\n"),
            PlannedArtifact.CreateBinary("logo.png", [1, 2, 3])
        ];
        _unchanged = [.. ServiceLayout.Apply(_default, _artifacts)];
        _moved = [.. ServiceLayout.Apply(_layout, _artifacts)];
    }

    [Fact] void should_leave_the_artifacts_unchanged_without_the_layout() => Describe(_unchanged).ShouldEqual(Describe(_artifacts));
    [Fact] void should_have_no_folder_without_the_layout() => ServiceLayout.Folder(_default).ShouldEqual(string.Empty);
    [Fact] void should_use_the_project_name_as_the_folder() => ServiceLayout.Folder(_layout).ShouldEqual("Source/Projects/");
    [Fact] void should_prefix_every_kept_artifact() => _moved.All(artifact => artifact.RelativePath.StartsWith("Source/Projects/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_application_files() => _moved.Select(artifact => artifact.RelativePath).ShouldContain("Source/Projects/Program.cs");
    [Fact] void should_drop_the_repository_files() => _moved.Select(artifact => artifact.RelativePath).Where(path => path is "Source/Projects/.editorconfig" or "Source/Projects/Directory.Build.props" or "Source/Projects/Directory.Build.targets" or "Source/Projects/Directory.Packages.props" or "Source/Projects/Projects.slnx" or "Source/Projects/docker-compose.yml").ShouldBeEmpty();
    [Fact] void should_keep_files_with_the_same_names_in_a_sub_folder() => _moved.Select(artifact => artifact.RelativePath).ShouldContain("Source/Projects/Orders/Directory.Build.props");
    [Fact] void should_keep_the_same_names_in_a_sub_folder_for_compose() => _moved.Select(artifact => artifact.RelativePath).ShouldContain("Source/Projects/Orders/docker-compose.yml");
    [Fact] void should_keep_the_bytes() => _moved.Single(artifact => artifact.RelativePath == "Source/Projects/Program.cs").Sha256.ShouldEqual(PlannedArtifact.CreateText("Source/Projects/Program.cs", "var a = 1;\n").Sha256);
    [Fact] void should_keep_binary_artifacts_binary() => _moved.Single(artifact => artifact.RelativePath == "Source/Projects/logo.png").Kind.ShouldEqual(PlannedArtifactKind.Binary);
    [Fact] void should_find_the_shared_folder() => ServiceLayout.SharedFolder(_moved.Select(artifact => artifact.RelativePath)).ShouldEqual("Source/Projects/");
    [Fact] void should_find_no_shared_folder_for_an_unplaced_plan() => ServiceLayout.SharedFolder(_artifacts.Select(artifact => artifact.RelativePath)).ShouldEqual(string.Empty);

    static string Describe(IEnumerable<PlannedArtifact> artifacts) => string.Join("\n", artifacts.Select(artifact => $"{artifact.RelativePath}:{artifact.Sha256}"));
}
