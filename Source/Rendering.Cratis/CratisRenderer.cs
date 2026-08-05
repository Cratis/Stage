// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Renders a compiled Screenplay application into a Cratis Arc + Chronicle vertical-slice application — the
/// Cratis-specific implementation of <see cref="IRenderer"/>. Scaffolds the target project on first use, then
/// renders every concept, composite type and slice, continuing best-effort on a per-item failure rather than
/// aborting the whole run.
/// </summary>
/// <param name="scaffolder">Scaffolds the target project when none exists yet.</param>
/// <param name="sliceRenderers">The <see cref="ISliceRenderer"/> to use per <see cref="SliceType"/>.</param>
/// <param name="codeOutput">Writes rendered files to their destination.</param>
public class CratisRenderer(IProjectScaffolder scaffolder, IReadOnlyDictionary<SliceType, ISliceRenderer> sliceRenderers, ICodeOutput codeOutput) : IRenderer
{
    /// <summary>
    /// Creates a <see cref="CratisRenderer"/> wired with the real scaffolder, slice renderers, and local file
    /// system output.
    /// </summary>
    /// <returns>The <see cref="CratisRenderer"/>.</returns>
    public static CratisRenderer CreateDefault()
    {
        var reactorRenderer = new ReactorSliceRenderer();
        var sliceRenderers = new Dictionary<SliceType, ISliceRenderer>
        {
            [SliceType.StateChange] = new StateChangeSliceRenderer(),
            [SliceType.StateView] = new StateViewSliceRenderer(),
            [SliceType.Automation] = reactorRenderer,
            [SliceType.Translate] = reactorRenderer,
        };

        return new CratisRenderer(new TemplateEngineProjectScaffolder(), sliceRenderers, new LocalFileSystemOutput());
    }

    /// <inheritdoc/>
    public async Task Render(IReadOnlyList<ApplicationSyntax> applications, DirectoryInfo targetDirectory, TextWriter output, TextWriter error)
    {
        await output.WriteLineAsync($"Rendering {applications.Count} application(s) to '{targetDirectory.FullName}'...");

        await scaffolder.EnsureScaffolded(targetDirectory, output);

        var applicationSet = new ApplicationSet(applications);
        var rootNamespace = Identifiers.ToPascalCase(targetDirectory.Name);

        foreach (var concept in applicationSet.Concepts.Values)
        {
            await WriteFile(ConceptRenderer.Render(concept, applicationSet, rootNamespace), targetDirectory, output, error);
        }

        foreach (var type in applicationSet.Types.Values)
        {
            await WriteFile(TypeRenderer.Render(type, applicationSet, rootNamespace), targetDirectory, output, error);
        }

        foreach (var slice in applicationSet.Slices)
        {
            await RenderSlice(slice, applicationSet, rootNamespace, targetDirectory, output, error);
        }

        await output.WriteLineAsync("Rendering complete.");
    }

    async Task RenderSlice(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace, DirectoryInfo targetDirectory, TextWriter output, TextWriter error)
    {
        var slicePath = string.Join('.', slice.FullPath);

        if (!sliceRenderers.TryGetValue(slice.Slice.Type, out var renderer))
        {
            await error.WriteLineAsync($"No renderer registered for slice type '{slice.Slice.Type}' ('{slicePath}') — skipped.");
            return;
        }

        try
        {
            await output.WriteLineAsync($"Rendering slice '{slicePath}'...");
            var file = renderer.Render(slice, applicationSet, rootNamespace);
            await WriteFile(file, targetDirectory, output, error);
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"Failed to render slice '{slicePath}': {exception.Message}");
        }
    }

    async Task WriteFile(RenderedFile file, DirectoryInfo targetDirectory, TextWriter output, TextWriter error)
    {
        try
        {
            await codeOutput.Write(file, targetDirectory, output);
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"Failed to write '{file.RelativePath}': {exception.Message}");
        }
    }
}
