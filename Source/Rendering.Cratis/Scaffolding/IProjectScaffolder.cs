// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Scaffolds a new target application when none exists yet.
/// </summary>
public interface IProjectScaffolder
{
    /// <summary>
    /// Scaffolds the target directory if it does not already contain a project.
    /// </summary>
    /// <param name="targetDirectory">The directory to scaffold into.</param>
    /// <param name="projectName">The project name — also the root namespace the rendered slices are placed in.</param>
    /// <param name="output">The <see cref="TextWriter"/> progress is reported to.</param>
    /// <returns><see langword="true"/> if scaffolding was performed; <see langword="false"/> if a project already existed.</returns>
    Task<bool> EnsureScaffolded(DirectoryInfo targetDirectory, string projectName, TextWriter output);
}
