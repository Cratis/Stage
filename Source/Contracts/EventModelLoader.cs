// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Loads an <see cref="EventModel"/> from Screenplay <c>.play</c> source fed to the engine at startup. Every
/// <c>.play</c> file beneath a directory is compiled and merged into a single model.
/// </summary>
public static class EventModelLoader
{
    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath the given directory (using the <c>**/*.play</c> glob) and
    /// merges them into a single <see cref="EventModel"/>.
    /// </summary>
    /// <param name="directory">The directory to search for <c>.play</c> files.</param>
    /// <returns>The compiled <see cref="EventModel"/>.</returns>
    /// <exception cref="InvalidEventModel">Thrown when the directory is missing, contains no <c>.play</c> files, or any file fails to compile.</exception>
    public static Task<EventModel> LoadFromDirectoryAsync(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new InvalidEventModel(directory);
        }

        var compilations = new PlayFileCompiler().CompileIn(directory).ToArray();
        if (compilations.Length == 0)
        {
            throw new InvalidEventModel(directory, ["No .play files were found."]);
        }

        var failures = compilations
            .Where(compilation => !compilation.Result.Success)
            .SelectMany(compilation => Errors(compilation.File.RelativePath, compilation.Result.Diagnostics))
            .ToArray();

        if (failures.Length > 0)
        {
            throw new InvalidEventModel(directory, failures);
        }

        var merged = Merge(compilations.Select(compilation => compilation.Result.Value!));

        return Task.FromResult(new ScreenplayEventModelVisitor().Visit(merged));
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

    static IEnumerable<string> Errors(string file, IEnumerable<Diagnostic> diagnostics) =>
        diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => $"{file}({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Message}");

    static ApplicationSyntax Merge(IEnumerable<ApplicationSyntax> applications)
    {
        var list = applications.ToArray();

        return new ApplicationSyntax(
            [.. list.SelectMany(application => application.Imports)],
            [.. list.SelectMany(application => application.Concepts)],
            [.. list.SelectMany(application => application.Policies)],
            [.. list.SelectMany(application => application.Modules)],
            SourceLocation.Start);
    }
}
