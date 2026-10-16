// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog.given;

/// <summary>
/// Restores and builds the current scaffold once per test run and generates the catalog from it.
/// </summary>
public class a_restored_scaffold : Specification
{
    static readonly Lazy<Task<(string Text, ScaffoldedProject Application, ScaffoldedProject Specifications)>> _generated = new(Generate);

    protected string _generatedText = null!;
    internal IReadOnlyList<ScaffoldedReference> References { get; set; } = null!;

    async Task Establish()
    {
        var restored = await _generated.Value;
        _generatedText = restored.Text;
        References = [.. restored.Application.References, .. restored.Specifications.References];
    }

    internal static string Workspace() => Environment.GetEnvironmentVariable("AI_WORK_OUTPUT") ?? Path.Combine(RepositoryRoot().FullName, ".ai-work", "stage-namespace");

    internal static async Task<(ScaffoldedProject Application, ScaffoldedProject Specifications)> Projects()
    {
        var restored = await _generated.Value;
        return (restored.Application, restored.Specifications);
    }

    /// <summary>
    /// Gets the repository root.
    /// </summary>
    /// <returns>The directory containing <c language="csharp">.git</c>.</returns>
    /// <exception cref="DirectoryNotFoundException">The specifications do not run inside a repository.</exception>
    internal static DirectoryInfo RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".git")) || Directory.Exists(Path.Combine(directory.FullName, ".git"))) return directory;
        }

        throw new DirectoryNotFoundException("The repository root containing .git was not found.");
    }

    static async Task<(string Text, ScaffoldedProject Application, ScaffoldedProject Specifications)> Generate()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Workspace(), $"name-catalog-{Guid.NewGuid():N}"));
        try
        {
            var (application, specifications) = await CatalogScaffold.Restore(directory);
            return (FrameworkNameCatalogText.Write(FrameworkNameCatalogGenerator.Generate(application, specifications)),
                application, specifications);
        }
        finally
        {
            // Delete only this run's scaffold, never sibling invocations.
            directory.Delete(recursive: true);
        }
    }
}
