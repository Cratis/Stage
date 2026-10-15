// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog.given;

/// <summary>
/// Restores and builds the current scaffold once per test run and generates the catalog from it.
/// </summary>
public class a_restored_scaffold : Specification
{
    static readonly Lazy<Task<string>> _generated = new(Generate);

    protected string _generatedText = null!;

    async Task Establish() => _generatedText = await _generated.Value;

    /// <summary>
    /// Gets the repository root.
    /// </summary>
    /// <returns>The directory containing <c language="csharp">.git</c>.</returns>
    /// <exception cref="DirectoryNotFoundException">The specifications do not run inside a repository.</exception>
    protected static DirectoryInfo RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".git")) || Directory.Exists(Path.Combine(directory.FullName, ".git"))) return directory;
        }

        throw new DirectoryNotFoundException("The repository root containing .git was not found.");
    }

    static async Task<string> Generate()
    {
        var workspace = Environment.GetEnvironmentVariable("AI_WORK_OUTPUT") ?? Path.Combine(RepositoryRoot().FullName, ".ai-work", "stage-namespace");
        var directory = Directory.CreateDirectory(Path.Combine(workspace, $"name-catalog-{Guid.NewGuid():N}"));
        try
        {
            var (application, specifications) = await CatalogScaffold.Restore(directory);
            return FrameworkNameCatalogText.Write(FrameworkNameCatalogGenerator.Generate(application, specifications));
        }
        finally
        {
            // Delete only this run's scaffold, never sibling invocations.
            directory.Delete(recursive: true);
        }
    }
}
