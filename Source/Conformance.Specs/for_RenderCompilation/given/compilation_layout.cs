// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Xml.Linq;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;

public sealed record compilation_layout(string Solution, IReadOnlyList<string> Projects, IReadOnlyList<string> SpecificationProjects, IReadOnlyList<string> SpecificationSources)
{
    public static compilation_layout Inspect(ArtifactRenderPlan plan)
    {
        var solution = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(".slnx", StringComparison.Ordinal));
        var projects = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".csproj", StringComparison.Ordinal)).ToArray();
        var references = Xml(solution).Descendants("Project")
            .Select(project => Normalize(Path.Combine(Path.GetDirectoryName(solution.RelativePath) ?? string.Empty, project.Attribute("Path")!.Value)))
            .Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(projects.Select(project => project.RelativePath).Order(StringComparer.Ordinal), references);

        // #753 keeps specs beside slices: the application's Debug project is itself a specification project.
        var specificationProjects = projects.Where(project => Xml(project).Descendants("IsTestProject").Any(property =>
            property.Value == "true" && (property.Parent?.Attribute("Condition")?.Value is not { } condition || condition.Contains("Debug", StringComparison.Ordinal))))
            .Select(project => project.RelativePath).ToArray();
        var specifications = plan.Artifacts.Where(artifact => IsSpecification(artifact.RelativePath)).Select(artifact => artifact.RelativePath).ToArray();
        foreach (var source in specifications)
        {
            var owner = projects.Where(project => source.StartsWith(ProjectDirectory(project.RelativePath), StringComparison.Ordinal))
                .OrderByDescending(project => ProjectDirectory(project.RelativePath).Length).First();
            Assert.Contains(owner.RelativePath, specificationProjects);
        }

        return new(solution.RelativePath, references, specificationProjects, specifications);
    }

    public void AssertComplete()
    {
        Assert.NotEmpty(Projects);
        Assert.NotEmpty(SpecificationProjects);
        Assert.NotEmpty(SpecificationSources);
        Assert.All(SpecificationProjects, project => Assert.Contains(project, Projects));
    }

    static XDocument Xml(PlannedArtifact artifact) => XDocument.Parse(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()));
    static string ProjectDirectory(string path) => Path.GetDirectoryName(path) is { Length: > 0 } directory ? Normalize(directory) + "/" : string.Empty;
    static string Normalize(string path) => path.Replace('\\', '/');
    static bool IsSpecification(string path) => path.EndsWith(".cs", StringComparison.Ordinal) &&
        (Path.GetFileName(path).StartsWith("when_", StringComparison.Ordinal) || path.Split('/').Any(part => part.StartsWith("for_", StringComparison.Ordinal) || part == "given"));
}
#endif
