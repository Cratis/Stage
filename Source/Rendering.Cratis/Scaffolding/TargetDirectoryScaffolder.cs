// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Makes sure the target directory exists, and scaffolds nothing into it.
/// </summary>
/// <remarks>
/// <para>
/// The default, because rendering into a project that already exists is the common case — regenerating from
/// a document that has moved on. Rendering needs somewhere to write and nothing more; a rendered application
/// is 33 files for a document the size of the language's own <c>invoicing.play</c>, and every one of them
/// lands here whether or not a project file surrounds them.
/// </para>
/// <para>
/// Scaffolding a project around them is a separate job, and an expensive one to carry: it needs the template
/// engine, which brings the whole NuGet client with it, which is what made this package impossible to host
/// beside anything that uses MSBuild (<see href="https://github.com/Cratis/Stage/issues/34">Cratis/Stage#34</see>).
/// A caller that wants it takes <c>Cratis.Stage.Rendering.Cratis.Scaffolding</c> and passes its
/// <c>TemplateEngineProjectScaffolder</c> in.
/// </para>
/// </remarks>
public class TargetDirectoryScaffolder : IProjectScaffolder
{
    /// <inheritdoc/>
    public Task<bool> EnsureScaffolded(DirectoryInfo targetDirectory, string projectName, TextWriter output)
    {
        targetDirectory.Create();
        return Task.FromResult(false);
    }
}
