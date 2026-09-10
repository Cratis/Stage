// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Packages;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Loads an <see cref="EventModel"/>, a <see cref="SceneApplication"/> or an <see cref="ApplicationRenderPlan"/>
/// from Screenplay <c>.play</c> source fed to the engine at startup. Every <c>.play</c> file beneath a
/// directory is compiled and merged into a single model.
/// </summary>
public static class EventModelLoader
{
    /// <summary>
    /// Compiles one Screenplay <c>.play</c> file or a directory of files into an <see cref="EventModel"/>.
    /// </summary>
    /// <param name="path">An existing <c>.play</c> file or directory containing <c>.play</c> files.</param>
    /// <returns>The compiled <see cref="EventModel"/>.</returns>
    /// <exception cref="InvalidEventModel">Thrown when the input is missing, unsupported, empty, or fails to compile.</exception>
    public static async Task<EventModel> LoadFromPathAsync(string path)
    {
        if (Directory.Exists(path))
        {
            return await LoadFromDirectoryAsync(path);
        }

        if (!File.Exists(path))
        {
            throw new InvalidEventModel(path, ["The path does not exist. Supply an existing .play file or a directory containing .play files."]);
        }

        if (!string.Equals(Path.GetExtension(path), ".play", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidEventModel(path, ["The selected file must have a .play extension. Supply a .play file or a directory containing .play files."]);
        }

        var compilation = new PlayFileCompiler().CompileFile(path, new ScreenplayEventModelVisitor());
        if (!compilation.Result.Success)
        {
            throw new InvalidEventModel(path, Errors(Path.GetFileName(path), compilation.Result.Diagnostics));
        }

        return compilation.Result.Value!;
    }

    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath the given directory (using the <c>**/*.play</c> glob) and
    /// merges them into a single <see cref="EventModel"/>.
    /// </summary>
    /// <param name="directory">The directory to search for <c>.play</c> files.</param>
    /// <returns>The compiled <see cref="EventModel"/>.</returns>
    /// <exception cref="InvalidEventModel">Thrown when the directory is missing, contains no <c>.play</c> files, or any file fails to compile.</exception>
    public static async Task<EventModel> LoadFromDirectoryAsync(string directory)
    {
        var merged = await CompileDirectory(directory);
        return new ScreenplayEventModelVisitor().Visit(merged);
    }

    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath the given directory (using the <c>**/*.play</c> glob), merges
    /// them into a single application, and translates it into a <see cref="SceneApplication"/> - the Screenplay-to-Scene
    /// seam (Cratis/Stage#37).
    /// </summary>
    /// <param name="directory">The directory to search for <c>.play</c> files.</param>
    /// <returns>The translated <see cref="SceneApplication"/>.</returns>
    /// <exception cref="InvalidEventModel">Thrown when the directory is missing, contains no <c>.play</c> files, or any file fails to compile.</exception>
    public static async Task<SceneApplication> LoadSceneApplicationFromDirectoryAsync(string directory)
    {
        var merged = await CompileDirectory(directory);
        return new ScreenplaySceneVisitor().Visit(merged);
    }

    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath the given directory (using the <c>**/*.play</c> glob), merges
    /// them into a single application, translates it into a <see cref="SceneApplication"/> and resolves that against the
    /// given package catalog - one <see cref="RenderPlan"/> per deployment target (Cratis/Stage#39).
    /// </summary>
    /// <param name="directory">The directory to search for <c>.play</c> files.</param>
    /// <param name="catalog">Every package available to resolve against - the declarations behind the names a <c>ui profile</c> lists.</param>
    /// <returns>The <see cref="ApplicationRenderPlan"/> for every target the application ships.</returns>
    /// <exception cref="InvalidEventModel">Thrown when the directory is missing, contains no <c>.play</c> files, or any file fails to compile.</exception>
    /// <remarks>
    /// Note where the two kinds of problem separate. A <em>compilation</em> error throws
    /// <see cref="InvalidEventModel"/> here, before anything is translated or resolved - source that does not
    /// compile has no model to plan. Everything discovered after that point is a <em>resolution</em> outcome:
    /// valid source that turns out to be under-specified for one concrete target. Those never throw; they are
    /// reported per target on the returned plan as <see cref="RenderFinding"/>s, so a caller sees every target's
    /// problems in one pass and decides for itself which ones stop a build.
    /// </remarks>
    public static async Task<ApplicationRenderPlan> LoadRenderPlanFromDirectoryAsync(string directory, IReadOnlyList<ScenePackage> catalog)
    {
        var merged = await CompileDirectory(directory);
        return RenderPlanner.Plan(new ScreenplaySceneVisitor().Visit(merged), catalog);
    }

    /// <summary>
    /// Compiles a single Screenplay document from source text into an <see cref="EventModel"/>.
    /// </summary>
    /// <param name="source">The Screenplay source text.</param>
    /// <returns>The compiled <see cref="EventModel"/>.</returns>
    /// <exception cref="InvalidEventModel">Thrown when the source fails to compile.</exception>
    public static EventModel LoadFromSource(string source)
    {
        var result = new ScreenplayCompiler().Compile(source);
        if (!result.Success)
        {
            throw new InvalidEventModel("<source>", Errors("<source>", result.Diagnostics));
        }

        return new ScreenplayEventModelVisitor().Visit(result.Value!);
    }

    static Task<ApplicationSyntax> CompileDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new InvalidEventModel(directory, ["The directory does not exist. Supply a directory containing .play files."]);
        }

        var compilation = new PlayFileCompiler().CompileFolder(directory);
        if (!compilation.Sources.Any())
        {
            throw new InvalidEventModel(directory, ["No .play files were found. Supply a directory containing .play files."]);
        }

        if (!compilation.Result.Success)
        {
            throw new InvalidEventModel(directory, Errors(Path.GetFileName(directory), compilation.Result.Diagnostics));
        }

        return Task.FromResult(compilation.Result.Value!);
    }

    static IEnumerable<string> Errors(string file, IEnumerable<Diagnostic> diagnostics) =>
        diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .OrderBy(diagnostic => diagnostic.Location.Path ?? file, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.Line)
            .ThenBy(diagnostic => diagnostic.Location.Column)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .Select(diagnostic => $"{diagnostic.Location.Path ?? file}({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Message}");
}
