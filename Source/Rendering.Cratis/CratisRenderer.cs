// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Renders a compiled Screenplay application into a Cratis Arc + Chronicle vertical-slice application — the
/// Cratis-specific implementation of <see cref="IRenderer"/>. Independent artifacts continue after a failure so
/// diagnostics are complete, but any blocking failure ends the operation with <see cref="RenderingFailed"/>.
/// </summary>
/// <param name="scaffolder">Scaffolds the target project when none exists yet.</param>
/// <param name="sliceRenderers">The <see cref="ISliceRenderer"/> to use per <see cref="SliceType"/>.</param>
/// <param name="codeOutput">Writes rendered files to their destination.</param>
public class CratisRenderer(IProjectScaffolder scaffolder, IReadOnlyDictionary<SliceType, ISliceRenderer> sliceRenderers, ICodeOutput codeOutput) : IRenderer
{
    /// <summary>
    /// Creates a <see cref="CratisRenderer"/> wired with the slice renderers and local file system output,
    /// rendering into the target directory without scaffolding a project around it.
    /// </summary>
    /// <remarks>
    /// Pass a <see cref="IProjectScaffolder"/> to the constructor to scaffold as well — the template-engine
    /// one lives in <c>Cratis.Stage.Rendering.Cratis.Scaffolding</c>, kept out of this package because the
    /// engine it needs cannot be hosted beside MSBuild.
    /// </remarks>
    /// <returns>The <see cref="CratisRenderer"/>.</returns>
    public static CratisRenderer CreateDefault()
    {
        var reactionRenderer = new ReactionSliceRenderer();
        var sliceRenderers = new Dictionary<SliceType, ISliceRenderer>
        {
            [SliceType.StateChange] = new StateChangeSliceRenderer(),
            [SliceType.StateView] = new StateViewSliceRenderer(),
            [SliceType.Automation] = reactionRenderer,
            [SliceType.Translate] = reactionRenderer,
        };

        return new CratisRenderer(new TargetDirectoryScaffolder(), sliceRenderers, new LocalFileSystemOutput());
    }

    /// <inheritdoc/>
    public async Task Render(IReadOnlyList<ApplicationSyntax> applications, DirectoryInfo targetDirectory, TextWriter output, TextWriter error)
    {
        var failures = new List<Exception>();
        await output.WriteLineAsync($"Rendering {applications.Count} application(s) to '{targetDirectory.FullName}'...");

        var rootNamespace = Identifiers.ToPascalCase(targetDirectory.Name);
        if (!await TryScaffold(targetDirectory, rootNamespace, output, error, failures))
        {
            await Complete(targetDirectory, output, error, failures);
            return;
        }

        var applicationSet = new ApplicationSet(applications);

        foreach (var concept in applicationSet.Concepts.Values)
        {
            await RenderFile(
                () => ConceptRenderer.Render(concept, applicationSet, rootNamespace),
                $"concept '{concept.Name}'",
                targetDirectory,
                output,
                error,
                failures);
        }

        foreach (var type in applicationSet.Types.Values)
        {
            await RenderFile(
                () => TypeRenderer.Render(type, applicationSet, rootNamespace),
                $"type '{type.Name}'",
                targetDirectory,
                output,
                error,
                failures);
        }

        foreach (var slice in applicationSet.Slices)
        {
            await RenderSlice(slice, applicationSet, rootNamespace, targetDirectory, output, error, failures);
        }

        await ReportUnrenderableReferences(applicationSet, error);
        await ReportUnrenderedDeclarations(applicationSet, error);
        await Complete(targetDirectory, output, error, failures);
    }

    /// <summary>
    /// Renders a single module — every slice under it, and nothing else.
    /// </summary>
    /// <param name="module">The <see cref="ModuleSyntax"/> to render.</param>
    /// <param name="context">The <see cref="ApplicationSet"/> giving the surrounding concepts, types and placements the slices resolve against.</param>
    /// <param name="targetDirectory">The directory to render into.</param>
    /// <param name="output">The <see cref="TextWriter"/> progress is reported to.</param>
    /// <param name="error">The <see cref="TextWriter"/> rendering problems are reported to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Render(ModuleSyntax module, ApplicationSet context, DirectoryInfo targetDirectory, TextWriter output, TextWriter error) =>
        RenderLocated([.. module.Locate()], context, targetDirectory, output, error);

    /// <summary>
    /// Renders a single feature — every slice under it, and nothing else.
    /// </summary>
    /// <param name="feature">The <see cref="FeatureSyntax"/> to render.</param>
    /// <param name="context">The <see cref="ApplicationSet"/> giving the surrounding concepts, types and placements the slices resolve against.</param>
    /// <param name="targetDirectory">The directory to render into.</param>
    /// <param name="output">The <see cref="TextWriter"/> progress is reported to.</param>
    /// <param name="error">The <see cref="TextWriter"/> rendering problems are reported to.</param>
    /// <param name="module">The module the feature belongs to; omitted, the feature is rendered directly in the target directory.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Render(
        FeatureSyntax feature, ApplicationSet context, DirectoryInfo targetDirectory, TextWriter output, TextWriter error, string? module = null) =>
        RenderLocated([.. feature.Locate(Path(module))], context, targetDirectory, output, error);

    /// <summary>
    /// Renders a single slice, and nothing else.
    /// </summary>
    /// <param name="slice">The <see cref="SliceSyntax"/> to render.</param>
    /// <param name="context">The <see cref="ApplicationSet"/> giving the surrounding concepts, types and placements the slice resolves against.</param>
    /// <param name="targetDirectory">The directory to render into.</param>
    /// <param name="output">The <see cref="TextWriter"/> progress is reported to.</param>
    /// <param name="error">The <see cref="TextWriter"/> rendering problems are reported to.</param>
    /// <param name="module">The module the slice belongs to; omitted, that level is left out of the folder and namespace.</param>
    /// <param name="feature">The feature the slice belongs to; omitted, that level is left out of the folder and namespace.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Render(
        SliceSyntax slice,
        ApplicationSet context,
        DirectoryInfo targetDirectory,
        TextWriter output,
        TextWriter error,
        string? module = null,
        string? feature = null) =>
        RenderLocated([new LocatedSlice(slice, Path(module, feature))], context, targetDirectory, output, error);

    static IReadOnlyList<string> Path(params string?[] segments) => [.. segments.Where(segment => !string.IsNullOrWhiteSpace(segment))!];

    /// <summary>
    /// Reports every name a slice references that nothing in the rendered output declares. An <c>import</c> names
    /// a construct owned by another domain — the rendered application has no source for it, and a reference to it
    /// will not compile until that domain's contracts are referenced.
    /// </summary>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> that was rendered.</param>
    /// <param name="error">The <see cref="TextWriter"/> rendering problems are reported to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    static async Task ReportUnrenderableReferences(ApplicationSet applicationSet, TextWriter error)
    {
        foreach (var name in applicationSet.ImportedNames.Where(name => !applicationSet.DeclarationPlacements.ContainsKey(name)).Order(StringComparer.Ordinal))
        {
            await error.WriteLineAsync(
                $"'{name}' is imported from another domain and is not declared here — references to it are rendered but will not compile until its contracts are referenced.");
        }
    }

    /// <summary>
    /// Reports the application-wide declarations nothing renders. A <c>persona</c> is what binds a caller to the
    /// policies they hold and <c>authentication</c> is what establishes who the caller is — without them the
    /// authorization the commands and read models now carry has nothing to evaluate against, so their absence is
    /// as load-bearing as a missing slice.
    /// </summary>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> that was rendered.</param>
    /// <param name="error">The <see cref="TextWriter"/> rendering problems are reported to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    static async Task ReportUnrenderedDeclarations(ApplicationSet applicationSet, TextWriter error)
    {
        var personas = applicationSet.Applications.Sum(application => application.Personas?.Count() ?? 0);
        if (personas > 0)
        {
            await error.WriteLineAsync(
                $"{personas} persona declaration(s) are not rendered — nothing maps a caller to the policies they hold.");
        }

        var seeds = applicationSet.Applications.Count(application => application.Seeds?.Any() == true);
        if (seeds > 0)
        {
            await error.WriteLineAsync($"{seeds} seed declaration(s) are not rendered — the application starts with no seeded data.");
        }

        var providers = applicationSet.Applications.Sum(application => application.Authentication?.Providers.Count() ?? 0);
        if (providers > 0)
        {
            await error.WriteLineAsync(
                $"{providers} authentication provider(s) are not rendered — the application authenticates nobody, so every authorization requirement rejects.");
        }
    }

    static async Task RecordFailure(string operation, Exception exception, TextWriter error, List<Exception> failures)
    {
        failures.Add(exception);
        await error.WriteLineAsync($"Failed to {operation}: {exception.Message}");
    }

    async Task RenderLocated(
        IReadOnlyList<LocatedSlice> slices, ApplicationSet context, DirectoryInfo targetDirectory, TextWriter output, TextWriter error)
    {
        var failures = new List<Exception>();
        var rootNamespace = Identifiers.ToPascalCase(targetDirectory.Name);
        if (!await TryScaffold(targetDirectory, rootNamespace, output, error, failures))
        {
            await Complete(targetDirectory, output, error, failures);
            return;
        }

        foreach (var slice in slices)
        {
            await RenderSlice(slice, context, rootNamespace, targetDirectory, output, error, failures);
        }

        await Complete(targetDirectory, output, error, failures);
    }

    async Task RenderSlice(
        LocatedSlice slice,
        ApplicationSet applicationSet,
        string rootNamespace,
        DirectoryInfo targetDirectory,
        TextWriter output,
        TextWriter error,
        List<Exception> failures)
    {
        var slicePath = string.Join('.', slice.FullPath);

        if (!sliceRenderers.TryGetValue(slice.Slice.Type, out var renderer))
        {
            await error.WriteLineAsync($"No renderer registered for slice type '{slice.Slice.Type}' ('{slicePath}') — skipped.");
            return;
        }

        await output.WriteLineAsync($"Rendering slice '{slicePath}'...");

        RenderedFile file;
        try
        {
            file = renderer.Render(slice, applicationSet, rootNamespace);
        }
        catch (Exception exception)
        {
            await RecordFailure($"render slice '{slicePath}'", exception, error, failures);
            return;
        }

        await WriteFile(file, targetDirectory, output, error, failures);
        await RenderSpecifications(slice, applicationSet, rootNamespace, targetDirectory, output, error, failures);
    }

    /// <summary>
    /// Renders the slice's specifications, one file each. A specification that cannot be rendered faithfully is
    /// reported rather than emitted — a spec asserting something the document did not state is worse than none.
    /// </summary>
    /// <param name="slice">The located slice whose specifications to render.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve against.</param>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <param name="targetDirectory">The directory to render into.</param>
    /// <param name="output">The <see cref="TextWriter"/> progress is reported to.</param>
    /// <param name="error">The <see cref="TextWriter"/> rendering problems are reported to.</param>
    /// <param name="failures">Collects blocking failures while independent artifacts continue.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task RenderSpecifications(
        LocatedSlice slice,
        ApplicationSet applicationSet,
        string rootNamespace,
        DirectoryInfo targetDirectory,
        TextWriter output,
        TextWriter error,
        List<Exception> failures)
    {
        foreach (var specification in slice.Slice.Specifications)
        {
            if (SpecificationRenderer.Unrenderable(specification, slice.Slice) is { } reason)
            {
                await error.WriteLineAsync($"Specification '{specification.Name}' is not rendered — {reason}.");
                continue;
            }

            await RenderFile(
                () => SpecificationRenderer.Render(specification, slice.Slice.Commands.First(), slice, applicationSet, rootNamespace),
                $"specification '{specification.Name}'",
                targetDirectory,
                output,
                error,
                failures);
        }
    }

    async Task RenderFile(
        Func<RenderedFile> render,
        string subject,
        DirectoryInfo targetDirectory,
        TextWriter output,
        TextWriter error,
        List<Exception> failures)
    {
        RenderedFile file;
        try
        {
            file = render();
        }
        catch (Exception exception)
        {
            await RecordFailure($"render {subject}", exception, error, failures);
            return;
        }

        await WriteFile(file, targetDirectory, output, error, failures);
    }

    async Task WriteFile(
        RenderedFile file,
        DirectoryInfo targetDirectory,
        TextWriter output,
        TextWriter error,
        List<Exception> failures)
    {
        try
        {
            foreach (var diagnostic in file.Diagnostics)
            {
                await error.WriteLineAsync($"{file.RelativePath}: {diagnostic}");
            }

            await codeOutput.Write(file, targetDirectory, output);
        }
        catch (Exception exception)
        {
            await RecordFailure($"write '{file.RelativePath}'", exception, error, failures);
        }
    }

    async Task<bool> TryScaffold(
        DirectoryInfo targetDirectory,
        string rootNamespace,
        TextWriter output,
        TextWriter error,
        List<Exception> failures)
    {
        try
        {
            await scaffolder.EnsureScaffolded(targetDirectory, rootNamespace, output);
            return true;
        }
        catch (Exception exception)
        {
            await RecordFailure("scaffold the target", exception, error, failures);
            return false;
        }
    }

    async Task Complete(DirectoryInfo targetDirectory, TextWriter output, TextWriter error, List<Exception> failures)
    {
        if (failures.Count == 0)
        {
            await output.WriteLineAsync("Rendering complete.");
            return;
        }

        await error.WriteLineAsync(
            $"Rendering failed with {failures.Count} blocking failure(s). The target output is unsafe and incomplete; " +
            "files from earlier runs may remain, including artifacts blocked by this run.");

        try
        {
            var markerWritten = await codeOutput.TryWriteFailureMarker(targetDirectory, output);
            await error.WriteLineAsync(markerWritten
                ? $"Wrote advisory failure marker '{RenderFailureMarker.RelativePath}'. It does not remove or disable stale artifacts."
                : $"No new failure marker was written at '{RenderFailureMarker.RelativePath}'. The output remains unsafe and incomplete.");
        }
        catch (Exception exception)
        {
            failures.Add(exception);
            await error.WriteLineAsync($"Failed to write advisory failure marker '{RenderFailureMarker.RelativePath}': {exception.Message}");
        }

        throw new RenderingFailed(failures);
    }
}
