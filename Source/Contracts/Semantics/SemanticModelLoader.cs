// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// The compiled executable model and its capability-admitted plan.
/// </summary>
/// <param name="Model">The executable semantic model.</param>
/// <param name="Plan">The execution plan.</param>
/// <remarks>Added attachment collections use the collection's reference equality in record comparisons.</remarks>
public sealed record LoadedSemanticModel(ExecutableSemanticModel Model, SemanticExecutionPlan Plan)
{
    /// <summary>
    /// Gets the source-bound specification origins from the compilation.
    /// </summary>
    public ImmutableDictionary<SemanticId, EffectiveSpecification> SpecificationOrigins { get; init; } = [];

    /// <summary>Requirements emitted by the compilation.</summary>
    public ImmutableArray<SemanticImplementationRequirement> ImplementationRequirements { get; init; } = [];

    /// <summary>Wrapper-ready context descriptors from the same compilation as <see cref="Model"/>.</summary>
    public ImmutableArray<SemanticTypedContextDescriptor> TypedContextDescriptors { get; init; } = [];

    /// <summary>Resolved inline and file bodies keyed by requirement identity.</summary>
    public ImmutableDictionary<string, string> ImplementationContents { get; init; } = [];

    /// <summary>Warnings from the attachment loader, including PLAY043x reasons for refused files.</summary>
    public ImmutableArray<Diagnostic> AttachmentDiagnostics { get; init; } = [];
}

/// <summary>
/// Describes an expected loading or compilation failure.
/// </summary>
/// <param name="Code">The stable STAGE-PLAN or original PLAY code.</param>
/// <param name="Message">The failure details.</param>
/// <param name="Source">The root-relative file and location, if available.</param>
public sealed record SemanticModelLoadDiagnostic(string Code, string Message, string? Source = null)
{
    /// <summary>
    /// Gets the original compiler severity, or error for a source-loading refusal.
    /// </summary>
    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Error;
}

/// <summary>
/// Carries either an executable source model or loading diagnostics.
/// </summary>
/// <param name="Loaded">The executable model, absent on failure.</param>
/// <param name="Diagnostics">The typed loading diagnostics.</param>
public sealed record SemanticModelLoadResult(LoadedSemanticModel? Loaded, ImmutableArray<SemanticModelLoadDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets whether loading and execution-plan admission succeeded.
    /// </summary>
    public bool Success => Loaded is not null;
}

/// <summary>
/// Loads portable Screenplay semantic execution from files and folders of source documents.
/// </summary>
public static class SemanticModelLoader
{
    /// <summary>
    /// Compiles a Screenplay source set and optional authoritative identity catalog.
    /// </summary>
    /// <param name="path">A .play file or a folder searched recursively for .play sources. For a single file, its parent directory is the attachment root.</param>
    /// <param name="catalogPath">An optional canonical Screenplay identity catalog JSON file whose document keys match the UTF-8 hex encoding of relative .play paths.</param>
    /// <remarks>Preserve the application name, root-relative source paths, and catalog to retain semantic identities.</remarks>
    /// <returns>The executable model and plan.</returns>
    /// <exception cref="InvalidSemanticModel">Input is absent, malformed, or not executable.</exception>
    public static Task<LoadedSemanticModel> LoadFromPathAsync(string path, string? catalogPath = null) => LoadFromPathAsync(path, catalogPath, null);

    /// <summary>
    /// Compiles a source set using an explicit application name when supplied.
    /// </summary>
    /// <param name="path">A .play file or directory.</param>
    /// <param name="catalogPath">An optional authoritative identity catalog.</param>
    /// <param name="applicationName">The application name; defaults to the folder or file name.</param>
    /// <returns>The executable model and plan.</returns>
    /// <exception cref="InvalidSemanticModel">The source cannot compile into an executable plan.</exception>
    public static async Task<LoadedSemanticModel> LoadFromPathAsync(string path, string? catalogPath, string? applicationName)
    {
        var isFolder = Directory.Exists(path);
        if (!isFolder && (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".play", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidSemanticModel(["Supply an existing .play file or a directory containing .play files."]);
        }
        var files = isFolder ? Directory.GetFiles(path, "*.play", SearchOption.AllDirectories) : [path];
        if (files.Length == 0) throw new InvalidSemanticModel(["No .play files were found."]);
        var root = isFolder ? path : Path.GetDirectoryName(Path.GetFullPath(path))!;
        var name = applicationName ?? (isFolder ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path));
        var catalog = catalogPath is null ? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(name)) :
            SemanticIdentityCatalogSerializer.Deserialize(await File.ReadAllBytesAsync(catalogPath));
        var documents = new List<SemanticSourceDocument>();
        foreach (var file in files.Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var key = Convert.ToHexString(Encoding.UTF8.GetBytes(relative));
            documents.Add(SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, relative, await File.ReadAllTextAsync(file)));
        }
        var result = Compile(root, name, catalog, [.. documents], default);
        if (!result.Success)
        {
            throw new InvalidSemanticModel([.. result.Diagnostics.Where(diagnostic => diagnostic.Code != "STAGE-PLAN-003")
                .Select(diagnostic => diagnostic.Source is null ? diagnostic.Message : $"{diagnostic.Source}: {diagnostic.Message}")]);
        }

        return result.Loaded!;
    }

    /// <summary>
    /// Compiles a deduplicated union of files and folders under a stable root without throwing for expected failures.
    /// </summary>
    /// <param name="root">The identity and attachment root.</param>
    /// <param name="paths">Absolute or root-relative files or folders; empty selects the whole root.</param>
    /// <param name="catalogPath">An optional absolute or root-relative authoritative identity catalog.</param>
    /// <param name="applicationName">The explicit semantic application name.</param>
    /// <param name="cancellationToken">Cancellation of source reads and compilation.</param>
    /// <returns>The loaded executable model or typed diagnostics.</returns>
    /// <exception cref="OperationCanceledException">Loading was canceled.</exception>
    public static async Task<SemanticModelLoadResult> LoadAsync(string root, ImmutableArray<string> paths, string? catalogPath, string applicationName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string[] files;
        try
        {
            root = Path.GetFullPath(root);
            var diagnostics = new List<SemanticModelLoadDiagnostic>();
            files = Discover(root, paths, diagnostics);
            if (diagnostics.Count > 0) return new(null, [.. diagnostics.Distinct()]);
            if (files.Length == 0) return Failure("STAGE-PLAN-002", "No .play files were found.");
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return Failure("STAGE-PLAN-001", exception.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failure("STAGE-PLAN-005", exception.Message);
        }
        SemanticIdentityCatalog catalog;
        var documents = new List<SemanticSourceDocument>();
        try
        {
            var catalogResult = await ReadCatalog(root, catalogPath, applicationName, cancellationToken);
            if (catalogResult.Catalog is null) return new(null, [catalogResult.Error!]);
            catalog = catalogResult.Catalog;
            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                var key = Convert.ToHexString(Encoding.UTF8.GetBytes(relative));
                try
                {
                    documents.Add(SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, relative, await File.ReadAllTextAsync(file, cancellationToken)));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    return Failure("STAGE-PLAN-005", exception.Message, relative);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failure("STAGE-PLAN-005", exception.Message);
        }

        try
        {
            return Compile(root, applicationName, catalog, [.. documents], cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failure("STAGE-PLAN-005", exception.Message);
        }
    }

    static string[] Discover(string root, ImmutableArray<string> paths, List<SemanticModelLoadDiagnostic> diagnostics)
    {
        var files = new List<string>();
        foreach (var path in (paths.IsDefaultOrEmpty ? [root] : paths).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var fullPath = Path.GetFullPath(path, root);
            var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
            if (relative == ".." || relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                diagnostics.Add(new("STAGE-PLAN-001", "A source must be under the supplied root.", relative));
            }
            else if (ContainsSourceLink(root, path))
            {
                diagnostics.Add(new("STAGE-PLAN-001", "Source paths under the supplied root must not contain links or reparse points.", relative));
            }
            else if (Directory.Exists(fullPath))
            {
                AddFolderFiles(root, fullPath, files, diagnostics);
            }
            else if (File.Exists(fullPath) && string.Equals(Path.GetExtension(fullPath), ".play", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(fullPath);
            }
            else
            {
                diagnostics.Add(new("STAGE-PLAN-001", "Supply an existing .play file or a directory containing .play files.", relative));
            }
        }

        return [.. files.Distinct(StringComparer.Ordinal).OrderBy(file => Path.GetRelativePath(root, file).Replace('\\', '/'), StringComparer.Ordinal)];
    }

    static void AddFolderFiles(string root, string folder, List<string> files, List<SemanticModelLoadDiagnostic> diagnostics)
    {
        var relative = Path.GetRelativePath(root, folder).Replace('\\', '/');
        try
        {
            if (ContainsSourceLink(root, folder))
            {
                diagnostics.Add(new("STAGE-PLAN-001", "Source directories under the supplied root must not contain links or reparse points.", relative));
                return;
            }
            foreach (var entry in Directory.GetFileSystemEntries(folder).Order(StringComparer.Ordinal))
            {
                if (ContainsSourceLink(root, entry))
                {
                    diagnostics.Add(new("STAGE-PLAN-001", "Source paths under the supplied root must not contain links or reparse points.", Path.GetRelativePath(root, entry).Replace('\\', '/')));
                }
                else if (Directory.Exists(entry))
                {
                    AddFolderFiles(root, entry, files, diagnostics);
                }
                else if (entry.EndsWith(".play", OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    files.Add(entry);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(new("STAGE-PLAN-005", exception.Message, relative));
        }
    }

    static bool ContainsSourceLink(string root, string path)
    {
        var pathRoot = Path.GetPathRoot(path)!;
        var current = Path.IsPathFullyQualified(path) ? pathRoot : root;
        if (!Path.IsPathFullyQualified(path) && Path.IsPathRooted(path) && Path.EndsInDirectorySeparator(pathRoot)) current = Path.GetPathRoot(root)!;
        var segments = path[pathRoot.Length..];

        // Inspect each segment before a later '..' can remove it. Root and its ancestors are trusted.
        foreach (var segment in segments.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.GetFullPath(Path.Combine(current, segment));
            var relative = Path.GetRelativePath(root, current).Replace('\\', '/');
            if (string.Equals(relative, ".", StringComparison.Ordinal) || string.Equals(relative, "..", StringComparison.Ordinal) || relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative)) continue;
            FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            if (info.LinkTarget is not null || (info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint))) return true;
        }

        return false;
    }

    static async Task<(SemanticIdentityCatalog? Catalog, SemanticModelLoadDiagnostic? Error)> ReadCatalog(string root, string? path, string name, CancellationToken cancellationToken)
    {
        if (path is null) return (SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(name)), null);
        var relative = path.Replace('\\', '/');
        try
        {
            var fullPath = Path.GetFullPath(path, root);
            relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
            return (SemanticIdentityCatalogSerializer.Deserialize(await File.ReadAllBytesAsync(fullPath, cancellationToken)), null);
        }
        catch (Exception exception) when (exception is JsonException or InvalidSemanticContract or FormatException or ArgumentException or NotSupportedException)
        {
            return (null, new("STAGE-PLAN-006", exception.Message, relative));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (null, new("STAGE-PLAN-005", exception.Message, relative));
        }
    }

    static SemanticModelLoadResult Compile(string root, string name, SemanticIdentityCatalog catalog, ImmutableArray<SemanticSourceDocument> documents, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var attachments = AttachmentFiles.Load(root, documents);
        var documentSet = SemanticDocumentSet.Create(documents, catalog, attachments.Contents);
        var compiled = new SemanticModelCompiler().Compile(name, documentSet);
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = compiled.Diagnostics.Select(diagnostic => new SemanticModelLoadDiagnostic(diagnostic.Code, diagnostic.Message, $"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column})") { Severity = diagnostic.Severity }).ToImmutableArray();
        if (!compiled.Success)
        {
            return new(null, [new("STAGE-PLAN-003", "Screenplay compilation failed."), .. diagnostics]);
        }
        var model = compiled.Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model);
        if (!plan.Success)
        {
            return new(null, [.. diagnostics, .. plan.Issues.Select(issue => new SemanticModelLoadDiagnostic("STAGE-PLAN-004", $"{issue.Artifact}: {issue.Details}"))]);
        }

        var loaded = new LoadedSemanticModel(model, plan.Plan!)
        {
            SpecificationOrigins = compiled.Value!.SpecificationOrigins,
            ImplementationRequirements = compiled.ImplementationRequirements,
            TypedContextDescriptors = compiled.TypedContextDescriptors,
            ImplementationContents = SemanticImplementationBodies.Resolve(documentSet, compiled.ImplementationRequirements),
            AttachmentDiagnostics = attachments.Diagnostics
        };

        return new(loaded, diagnostics);
    }

    static SemanticModelLoadResult Failure(string code, string message, string? source = null) => new(null, [new(code, message, source)]);
}
